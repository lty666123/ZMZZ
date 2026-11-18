$ErrorActionPreference = 'Stop'
$resumeRoot = 'C:\Users\ASUS\Documents\ChatGPT\Unity All\简历\投递终版_完整关键词版\云存档更新_20261118'
$resumeStem = '李天宇_Unity客户端实习简历_完整投递版'
$resumeWord = $null
$resumeDocument = $null
try {
    $resumeWord = New-Object -ComObject Word.Application
    $resumeWord.Visible = $false
    $resumeWord.DisplayAlerts = 0
    $resumeDocument = $resumeWord.Documents.Open((Join-Path $resumeRoot ($resumeStem + '.docx')), $false, $true)
    $resumeDocument.ExportAsFixedFormat((Join-Path $resumeRoot ($resumeStem + '.pdf')), 17)
    'PDF exported from the staged DOCX using Word.'
} finally {
    if ($null -ne $resumeDocument) {
        $resumeDocument.Close(0)
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($resumeDocument)
    }
    if ($null -ne $resumeWord) {
        $resumeWord.Quit()
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($resumeWord)
    }
}
