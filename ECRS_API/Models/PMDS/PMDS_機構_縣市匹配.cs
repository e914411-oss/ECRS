using System;
using System.Collections.Generic;

namespace ECRS_API.Models.PMDS;

public partial class PMDS_機構_縣市匹配
{
    public int 主鍵 { get; set; }

    public string? 機構縣市鄉鎮市區主鍵 { get; set; }

    public string? 機構縣市鄉鎮市區 { get; set; }

    public string? 縣市代碼 { get; set; }

    public string? 鄉鎮區代碼 { get; set; }
}
