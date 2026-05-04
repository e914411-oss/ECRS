using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.PMDS;

public partial class 業者_HACCP_對應表
{
    public int 主鍵 { get; set; }

    public int 業者主鍵 { get; set; }

    public string? 工廠主鍵 { get; set; }
}
