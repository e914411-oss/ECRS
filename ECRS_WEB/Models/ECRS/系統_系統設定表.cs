using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.ECRS;

public partial class 系統_系統設定表
{
    public int 主鍵 { get; set; }

    public string? 設定_索引鍵 { get; set; }

    public string? 設定_內容值 { get; set; }
}
