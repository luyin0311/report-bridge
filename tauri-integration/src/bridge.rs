/// 报表桥接模块
///
/// 管理 report-bridge.exe 的完整生命周期：
///   - 通过 Tauri Sidecar 启动进程
///   - 从 stdout 读取端口号
///   - 提供 HTTP 客户端方法调用所有报表 API
///   - App 退出时自动清理子进程
///
/// ## 集成步骤
///
/// ### 1. Cargo.toml 添加依赖
/// ```toml
/// [dependencies]
/// reqwest = { version = "0.12", features = ["json"] }
/// serde = { version = "1", features = ["derive"] }
/// serde_json = "1"
/// tokio = { version = "1", features = ["sync", "time"] }
/// ```
///
/// ### 2. tauri.conf.json 配置 Sidecar
/// ```json
/// {
///   "bundle": {
///     "externalBin": ["binaries/report-bridge.exe"]
///   }
/// }
/// ```
///
/// ### 3. 构建前将 report-bridge.exe 放入 src-tauri/binaries/
///
/// ### 4. main.rs 中注册
/// ```rust
/// mod bridge;
/// use bridge::ReportBridge;
/// use std::sync::Mutex;
/// use tauri::Manager;
///
/// fn main() {
///     tauri::Builder::default()
///         .manage(Mutex::new(ReportBridge::default()))
///         .setup(|app| {
///             // App 退出时清理
///             let handle = app.handle().clone();
///             Ok(())
///         })
///         // ... 注册 command
///         .run(tauri::generate_context!())
///         .expect("error while running tauri application");
/// }
/// ```

use reqwest::Client;
use serde::{Deserialize, Serialize};
use std::sync::Mutex;
use tauri::process::{Command as ProcessCommand, CommandEvent};
use tokio::sync::oneshot;

// ───────────────────────────────────────────
// 数据模型（与 C# 端 ApiModels.cs 一一对应）
// ───────────────────────────────────────────

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct ReportDataset {
    pub columns: Vec<String>,
    pub rows: Vec<Vec<serde_json::Value>>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct DesignRequest {
    pub template_path: String,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct DesignResult {
    pub saved_path: String,
    pub changed: bool,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct PreviewRequest {
    pub template_path: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub data: Option<ReportDataset>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct RenderRequest {
    pub template_path: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub data: Option<ReportDataset>,
    #[serde(default = "default_format")]
    pub format: String,
    pub output_path: String,
}

fn default_format() -> String {
    "pdf".to_string()
}

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct RenderResult {
    pub file_path: String,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct TemplateInfo {
    pub name: String,
    pub full_path: String,
    pub size: i64,
    pub last_modified: String,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct ApiResponse {
    success: bool,
    #[serde(default)]
    message: Option<String>,
    #[serde(default)]
    data: Option<serde_json::Value>,
}

// ───────────────────────────────────────────
// ReportBridge
// ───────────────────────────────────────────

pub struct ReportBridge {
    http: Client,
    base_url: Mutex<Option<String>>,
    /// 保存 sidecar 子进程句柄，Drop 时自动 kill
    child: Mutex<Option<tauri::process::CommandChild>>,
}

impl Default for ReportBridge {
    fn default() -> Self {
        // 不设置全局超时：设计/预览操作是用户交互驱动的，时长不可控（可能几分钟到几小时）。
        // 本地 127.0.0.1 通信不会出现真正的网络超时，如 Sidecar 崩溃可通过 ensure_started 检测。
        let http = Client::builder()
            .no_proxy()
            .build()
            .expect("failed to build reqwest client");

        Self {
            http,
            base_url: Mutex::new(None),
            child: Mutex::new(None),
        }
    }
}

impl ReportBridge {
    /// 启动 sidecar 并读取端口号（通过 stdout 第一行）
    pub async fn ensure_started(&self, app: tauri::AppHandle) -> Result<String, String> {
        // 已启动则直接返回
        {
            let base = self.base_url.lock().unwrap();
            if let Some(ref url) = *base {
                return Ok(url.clone());
            }
        }

        // 启动 sidecar
        let (mut rx, child) = ProcessCommand::sidecar("report-bridge")
            .map_err(|e| format!("找不到 sidecar: {}", e))?
            .spawn()
            .map_err(|e| format!("启动 sidecar 失败: {}", e))?;

        // 保存子进程句柄
        *self.child.lock().unwrap() = Some(child);

        // 读取端口号（stdout 第一行）
        let (tx, rx_port) = oneshot::channel::<String>();
        let mut tx = Some(tx);

        while let Some(event) = rx.recv().await {
            match event {
                CommandEvent::Stdout(line) => {
                    if let Some(tx) = tx.take() {
                        if let Ok(parsed) = serde_json::from_str::<serde_json::Value>(&line) {
                            if let Some(port) = parsed["port"].as_u64() {
                                let url = format!("http://127.0.0.1:{}", port);
                                let _ = tx.send(url);
                            }
                        }
                    }
                }
                CommandEvent::Stderr(line) => {
                    eprintln!("[Bridge stderr] {}", line);
                }
                CommandEvent::Terminated(status) => {
                    if tx.is_some() {
                        let _ = tx.take().unwrap().send("".to_string());
                    }
                    return Err(format!("Sidecar 意外退出, code: {:?}", status.code));
                }
                _ => {}
            }
        }

        match rx_port.await {
            Ok(url) if !url.is_empty() => {
                // 等待 HTTP 服务就绪
                for _ in 0..20 {
                    if let Ok(resp) = self.http.get(format!("{}/api/health", &url)).send().await {
                        if resp.status().is_success() {
                            *self.base_url.lock().unwrap() = Some(url.clone());
                            return Ok(url);
                        }
                    }
                    tokio::time::sleep(std::time::Duration::from_millis(200)).await;
                }
                Err("报表桥接 HTTP 服务启动超时".into())
            }
            _ => Err("未能读取到 sidecar 端口号".into()),
        }
    }

    /// 获取 base URL（必须已启动）
    fn get_base(&self) -> Result<String, String> {
        self.base_url
            .lock()
            .unwrap()
            .clone()
            .ok_or_else(|| "报表桥接未启动".into())
    }

    // ── API 方法 ──

    /// 健康检查
    pub async fn health(&self) -> Result<ApiResponse, String> {
        let url = format!("{}/api/health", self.get_base()?);
        let resp = self.http.get(&url).send().await.map_err(|e| e.to_string())?;
        resp.json().await.map_err(|e| e.to_string())
    }

    /// 列出模板
    pub async fn list_templates(&self, dir: &str) -> Result<Vec<TemplateInfo>, String> {
        let base = self.get_base()?;
        let url = format!("{}/api/templates?dir={}", base, urlencoding(dir));
        let resp = self.http.get(&url).send().await.map_err(|e| e.to_string())?;
        let api: ApiResponse = resp.json().await.map_err(|e| e.to_string())?;
        if api.success {
            if let Some(data) = api.data {
                let templates: Vec<TemplateInfo> =
                    serde_json::from_value(data["templates"].clone())
                        .map_err(|e| e.to_string())?;
                return Ok(templates);
            }
            Ok(vec![])
        } else {
            Err(api.message.unwrap_or_default())
        }
    }

    /// 删除模板
    pub async fn delete_template(&self, full_path: &str) -> Result<(), String> {
        let base = self.get_base()?;
        let url = format!("{}/api/templates/{}", base, urlencoding(full_path));
        let resp = self
            .http
            .delete(&url)
            .send()
            .await
            .map_err(|e| e.to_string())?;
        let api: ApiResponse = resp.json().await.map_err(|e| e.to_string())?;
        if api.success {
            Ok(())
        } else {
            Err(api.message.unwrap_or_default())
        }
    }

    /// 打开设计器（同步阻塞，会等待用户关闭窗口）
    pub async fn design(
        &self,
        template_path: &str,
    ) -> Result<DesignResult, String> {
        let url = format!("{}/api/design", self.get_base()?);
        let req = DesignRequest {
            template_path: template_path.to_string(),
        };
        let resp = self
            .http
            .post(&url)
            .json(&req)
            .send()
            .await
            .map_err(|e| e.to_string())?;
        let api: ApiResponse = resp.json().await.map_err(|e| e.to_string())?;
        if api.success {
            if let Some(data) = api.data {
                let result: DesignResult =
                    serde_json::from_value(data).map_err(|e| e.to_string())?;
                return Ok(result);
            }
            Err("设计器未返回结果".into())
        } else {
            Err(api.message.unwrap_or_default())
        }
    }

    /// 预览报表（同步阻塞）
    pub async fn preview(
        &self,
        template_path: &str,
        dataset: Option<ReportDataset>,
    ) -> Result<(), String> {
        let url = format!("{}/api/preview", self.get_base()?);
        let req = PreviewRequest {
            template_path: template_path.to_string(),
            data: dataset,
        };
        let resp = self
            .http
            .post(&url)
            .json(&req)
            .send()
            .await
            .map_err(|e| e.to_string())?;
        let api: ApiResponse = resp.json().await.map_err(|e| e.to_string())?;
        if api.success {
            Ok(())
        } else {
            Err(api.message.unwrap_or_default())
        }
    }

    /// 导出报表
    pub async fn render(
        &self,
        template_path: &str,
        dataset: Option<ReportDataset>,
        format: Option<&str>,
        output_path: &str,
    ) -> Result<RenderResult, String> {
        let url = format!("{}/api/render", self.get_base()?);
        let req = RenderRequest {
            template_path: template_path.to_string(),
            data: dataset,
            format: format.unwrap_or("pdf").to_string(),
            output_path: output_path.to_string(),
        };
        let resp = self
            .http
            .post(&url)
            .json(&req)
            .send()
            .await
            .map_err(|e| e.to_string())?;
        let api: ApiResponse = resp.json().await.map_err(|e| e.to_string())?;
        if api.success {
            if let Some(data) = api.data {
                let result: RenderResult =
                    serde_json::from_value(data).map_err(|e| e.to_string())?;
                return Ok(result);
            }
            Err("渲染未返回结果".into())
        } else {
            Err(api.message.unwrap_or_default())
        }
    }
}

impl Drop for ReportBridge {
    fn drop(&mut self) {
        if let Some(mut child) = self.child.lock().unwrap().take() {
            let _ = child.kill();
        }
    }
}

/// 简易 URL 编码（用于路径参数）
fn urlencoding(s: &str) -> String {
    percent_encoding(s)
}

fn percent_encoding(input: &str) -> String {
    let mut result = String::with_capacity(input.len());
    for byte in input.bytes() {
        match byte {
            b'A'..=b'Z' | b'a'..=b'z' | b'0'..=b'9' | b'-' | b'_' | b'.' | b'~' | b':' | b'\\' | b'/' => {
                result.push(byte as char)
            }
            b'%' => {
                result.push_str("%25");
            }
            other => {
                result.push_str(&format!("%{:02X}", other));
            }
        }
    }
    result
}

// ───────────────────────────────────────────
// Tauri Commands（注册到 main.rs 中）
// ───────────────────────────────────────────

/// 将 ReportBridge 和 AppHandle 注入为 Tauri State 后使用
/// 示例调用方式：
///
/// ```rust
/// fn main() {
///     tauri::Builder::default()
///         .manage(Mutex::new(ReportBridge::default()))
///         .invoke_handler(tauri::generate_handler![
///             bridge_ensure_started,
///             bridge_health,
///             bridge_design,
///             bridge_preview,
///             bridge_render,
///             bridge_list_templates,
///             bridge_delete_template,
///         ])
///         .run(tauri::generate_context!())
///         .expect("error");
/// }
/// ```

use std::sync::Mutex as StdMutex;

#[tauri::command]
pub async fn bridge_ensure_started(
    app: tauri::AppHandle,
    bridge: tauri::State<'_, StdMutex<ReportBridge>>,
) -> Result<String, String> {
    bridge.lock().unwrap().ensure_started(app).await
}

#[tauri::command]
pub async fn bridge_health(
    bridge: tauri::State<'_, StdMutex<ReportBridge>>,
) -> Result<ApiResponse, String> {
    bridge.lock().unwrap().health().await
}

#[tauri::command]
pub async fn bridge_design(
    template_path: String,
    bridge: tauri::State<'_, StdMutex<ReportBridge>>,
) -> Result<DesignResult, String> {
    bridge.lock().unwrap().design(&template_path).await
}

#[tauri::command]
pub async fn bridge_preview(
    template_path: String,
    data: Option<ReportDataset>,
    bridge: tauri::State<'_, StdMutex<ReportBridge>>,
) -> Result<(), String> {
    bridge.lock().unwrap().preview(&template_path, data).await
}

#[tauri::command]
pub async fn bridge_render(
    template_path: String,
    data: Option<ReportDataset>,
    format: Option<String>,
    output_path: String,
    bridge: tauri::State<'_, StdMutex<ReportBridge>>,
) -> Result<RenderResult, String> {
    bridge
        .lock()
        .unwrap()
        .render(&template_path, data, format.as_deref(), &output_path)
        .await
}

#[tauri::command]
pub async fn bridge_list_templates(
    dir: String,
    bridge: tauri::State<'_, StdMutex<ReportBridge>>,
) -> Result<Vec<TemplateInfo>, String> {
    bridge.lock().unwrap().list_templates(&dir).await
}

#[tauri::command]
pub async fn bridge_delete_template(
    full_path: String,
    bridge: tauri::State<'_, StdMutex<ReportBridge>>,
) -> Result<(), String> {
    bridge.lock().unwrap().delete_template(&full_path).await
}
