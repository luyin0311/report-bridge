using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ReportBridge.Models
{
    /// <summary>
    /// 统一 API 响应格式
    /// </summary>
    [DataContract]
    public class ApiResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }

        [DataMember(Name = "data", EmitDefaultValue = false)]
        public object Data { get; set; }

        public static ApiResponse Ok(object data = null)
        {
            return new ApiResponse { Success = true, Data = data };
        }

        public static ApiResponse Fail(string message)
        {
            return new ApiResponse { Success = false, Message = message };
        }
    }

    /// <summary>
    /// 数据集格式：列名 + 行数据
    /// </summary>
    [DataContract]
    public class ReportDataset
    {
        [DataMember(Name = "columns")]
        public List<string> Columns { get; set; }

        [DataMember(Name = "rows")]
        public List<List<object>> Rows { get; set; }
    }

    /// <summary>
    /// POST /api/design 请求
    /// </summary>
    [DataContract]
    public class DesignRequest
    {
        [DataMember(Name = "templatePath")]
        public string TemplatePath { get; set; }
    }

    /// <summary>
    /// /api/design 响应 data
    /// </summary>
    [DataContract]
    public class DesignResult
    {
        [DataMember(Name = "savedPath")]
        public string SavedPath { get; set; }

        [DataMember(Name = "changed")]
        public bool Changed { get; set; }
    }

    /// <summary>
    /// POST /api/preview 请求
    /// </summary>
    [DataContract]
    public class PreviewRequest
    {
        [DataMember(Name = "templatePath")]
        public string TemplatePath { get; set; }

        [DataMember(Name = "data")]
        public ReportDataset Data { get; set; }
    }

    /// <summary>
    /// POST /api/render 请求
    /// </summary>
    [DataContract]
    public class RenderRequest
    {
        [DataMember(Name = "templatePath")]
        public string TemplatePath { get; set; }

        [DataMember(Name = "data")]
        public ReportDataset Data { get; set; }

        [DataMember(Name = "format")]
        public string Format { get; set; } = "pdf";

        [DataMember(Name = "outputPath")]
        public string OutputPath { get; set; }
    }

    /// <summary>
    /// /api/render 响应 data
    /// </summary>
    [DataContract]
    public class RenderResult
    {
        [DataMember(Name = "filePath")]
        public string FilePath { get; set; }
    }

    /// <summary>
    /// 模板文件信息
    /// </summary>
    [DataContract]
    public class TemplateInfo
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "fullPath")]
        public string FullPath { get; set; }

        [DataMember(Name = "size")]
        public long Size { get; set; }

        [DataMember(Name = "lastModified")]
        public string LastModified { get; set; }
    }
}
