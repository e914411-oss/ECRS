using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.PMDS;

public partial class 來源文件保存稽查_專案清單
{
    public int 主鍵 { get; set; }

    public int 來源文件保存稽查主表_主鍵 { get; set; }

    public int 專案代碼 { get; set; }
}
