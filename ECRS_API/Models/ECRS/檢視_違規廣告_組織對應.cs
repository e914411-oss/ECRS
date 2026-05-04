using System;
using System.Collections.Generic;

namespace ECRS_API.Models.ECRS;

public partial class 檢視_違規廣告_組織對應
{
    public string 組織名稱 { get; set; } = null!;

    public string? 使用者帳號 { get; set; }

    public string? 姓名 { get; set; }
}
