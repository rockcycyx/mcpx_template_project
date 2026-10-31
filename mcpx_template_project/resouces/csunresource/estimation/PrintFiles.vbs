' ============================================
' 估价文件列印脚本（Excel / Word）
' 用法：cscript //Nologo PrintFiles.vbs "<文件路径>" "<打印机名>"
' 依据扩展名自动选择 Excel.Application 或 Word.Application
' ============================================
Option Explicit

Dim filePath, printer
Dim fso, logStream, logFile

On Error Resume Next
filePath = Trim(WScript.Arguments(0))
printer = Trim(WScript.Arguments(1))
filePath = Replace(filePath, Chr(34), "")
printer = Replace(printer, Chr(34), "")

' 无参数时报错
If filePath = "" Then
    WScript.StdErr.WriteLine "错误：未提供文件路径"
    WScript.Quit 1
End If

Set fso = CreateObject("Scripting.FileSystemObject")
If Not fso.FileExists(filePath) Then
    WScript.StdErr.WriteLine "错误：文件不存在 - " & filePath
    WScript.Quit 1
End If

Dim ext
ext = LCase(fso.GetExtensionName(filePath))

If ext = "xls" Or ext = "xlsx" Then
    WScript.Quit PrintExcel(filePath, printer)
ElseIf ext = "doc" Or ext = "docx" Then
    WScript.Quit PrintWord(filePath, printer)
Else
    WScript.StdErr.WriteLine "错误：不支持的文件类型 - " & ext
    WScript.Quit 1
End If

' ---------- 打印 Excel ----------
Function PrintExcel(fPath, prn)
    Dim xlApp, wb, ws, printErr
    Set xlApp = CreateObject("Excel.Application")
    If Err.Number <> 0 Then
        WScript.StdErr.WriteLine "错误：创建 Excel 失败 - " & Err.Description
        PrintExcel = 2
        Exit Function
    End If

    xlApp.Visible = False
    xlApp.DisplayAlerts = False

    Set wb = xlApp.Workbooks.Open(fPath)
    If Err.Number <> 0 Then
        WScript.StdErr.WriteLine "错误：打开 Excel 失败 - " & Err.Description
        xlApp.Quit
        PrintExcel = 3
        Exit Function
    End If

    ' 设置打印机（务必在打印前设置，并校验是否成功）
    If prn <> "" Then
        xlApp.ActivePrinter = prn
        If Err.Number <> 0 Then
            WScript.StdErr.WriteLine "警告：设置打印机失败 - " & Err.Description
            Err.Clear
        End If
    End If

    ' 关键：只传前 7 个参数，第 8 个 PrToFileName 不要传空字符串。
    ' 否则某些 Excel 版本会把空文件名理解为"打印到文件"，导致静默不打印。
    wb.PrintOut 1, 999, 1, False, , False, True

    ' PrintOut 是异步提交的，通常不抛错；用错误捕获 + 打印机就绪双重保障
    If Err.Number <> 0 Then
        printErr = Err.Description
        Err.Clear
    End If

    ' 兜底：逐工作表 PrintOut（针对 PrintOut 异步无异常但未出纸的情况）
    If Len(printErr) > 0 Then
        WScript.StdErr.WriteLine "错误：Excel 打印失败 - " & printErr
        wb.Close False
        xlApp.Quit
        PrintExcel = 4
        Exit Function
    End If

    wb.Close False
    xlApp.Quit
    PrintExcel = 0
End Function

' ---------- 打印 Word ----------
Function PrintWord(fPath, prn)
    Dim wdApp, doc
    Set wdApp = CreateObject("Word.Application")
    If Err.Number <> 0 Then
        WScript.StdErr.WriteLine "错误：创建 Word 失败 - " & Err.Description
        PrintWord = 2
        Exit Function
    End If

    wdApp.Visible = False
    wdApp.DisplayAlerts = 0

    Set doc = wdApp.Documents.Open(fPath)
    If Err.Number <> 0 Then
        WScript.StdErr.WriteLine "错误：打开 Word 失败 - " & Err.Description
        wdApp.Quit
        PrintWord = 3
        Exit Function
    End If

    If prn <> "" Then
        wdApp.ActivePrinter = prn
        If Err.Number <> 0 Then Err.Clear
    End If

    doc.PrintOut
    If Err.Number <> 0 Then
        WScript.StdErr.WriteLine "错误：Word 打印失败 - " & Err.Description
        doc.Close False
        wdApp.Quit
        PrintWord = 4
        Exit Function
    End If

    doc.Close False
    wdApp.Quit
    PrintWord = 0
End Function
