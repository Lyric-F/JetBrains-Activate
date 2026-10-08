$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskBitmap = New-Object System.Drawing.Bitmap 256,256
$taskGraphics = [System.Drawing.Graphics]::FromImage($taskBitmap)
$taskGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$taskGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$taskGraphics.Clear([System.Drawing.Color]::Transparent)
$taskPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$taskPath.AddArc(8,8,88,88,180,90)
$taskPath.AddArc(160,8,88,88,270,90)
$taskPath.AddArc(160,160,88,88,0,90)
$taskPath.AddArc(8,160,88,88,90,90)
$taskPath.CloseFigure()
$taskBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(32,59,80))
$taskGraphics.FillPath($taskBrush,$taskPath)
$taskFont = New-Object System.Drawing.Font 'Segoe UI',158,([System.Drawing.FontStyle]::Bold),([System.Drawing.GraphicsUnit]::Pixel)
$taskFormat = New-Object System.Drawing.StringFormat
$taskFormat.Alignment = [System.Drawing.StringAlignment]::Center
$taskFormat.LineAlignment = [System.Drawing.StringAlignment]::Center
$taskGraphics.DrawString('a',$taskFont,[System.Drawing.Brushes]::White,(New-Object System.Drawing.RectangleF 0,-16,256,256),$taskFormat)
$taskMemory = New-Object System.IO.MemoryStream
$taskBitmap.Save($taskMemory,[System.Drawing.Imaging.ImageFormat]::Png)
$taskPng = $taskMemory.ToArray()
$taskOutput = [IO.File]::Create((Join-Path $PSScriptRoot '..\resources\activate.ico'))
$taskWriter = New-Object System.IO.BinaryWriter $taskOutput
$taskWriter.Write([uint16]0); $taskWriter.Write([uint16]1); $taskWriter.Write([uint16]1)
$taskWriter.Write([byte]0); $taskWriter.Write([byte]0); $taskWriter.Write([byte]0); $taskWriter.Write([byte]0)
$taskWriter.Write([uint16]1); $taskWriter.Write([uint16]32); $taskWriter.Write([uint32]$taskPng.Length); $taskWriter.Write([uint32]22)
$taskWriter.Write($taskPng)
$taskWriter.Dispose(); $taskMemory.Dispose(); $taskFont.Dispose(); $taskFormat.Dispose(); $taskBrush.Dispose(); $taskPath.Dispose(); $taskGraphics.Dispose(); $taskBitmap.Dispose()
