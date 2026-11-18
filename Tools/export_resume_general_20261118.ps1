$ErrorActionPreference = 'Stop'
$resumeVariantRoot = 'C:\Users\ASUS\Documents\ChatGPT\Unity All\简历\投递终版_完整关键词版'
$resumeVariantStem = '李天宇_Unity客户端开发_简历'
$resumeVariantWord = $null
$resumeVariantDocument = $null
try {
    $resumeVariantWord = New-Object -ComObject Word.Application
    $resumeVariantWord.Visible = $false
    $resumeVariantWord.DisplayAlerts = 0
    $resumeVariantWord.AutomationSecurity = 3
    $resumeVariantDocument = $resumeVariantWord.Documents.Open((Join-Path $resumeVariantRoot ($resumeVariantStem + '.docx')), $false, $true)
    $resumeVariantDocument.ExportAsFixedFormat((Join-Path $resumeVariantRoot ($resumeVariantStem + '.pdf')), 17)
    'PDF exported from the new DOCX using Word.'
} finally {
    if ($null -ne $resumeVariantDocument) {
        $resumeVariantDocument.Close(0)
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($resumeVariantDocument)
    }
    if ($null -ne $resumeVariantWord) {
        $resumeVariantWord.Quit()
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($resumeVariantWord)
    }
}
