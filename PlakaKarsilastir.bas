Attribute VB_Name = "PlakaKarsilastir"
' Sayfa4!H sutunundaki plakalari emn!F sutunundaki plakalarla karsilastirir.
' Eslesen plakalari Sayfa4!S sutununa (ayni satira) renkli olarak yazar.
' Karsilastirmada bosluklar ve buyuk/kucuk harf farki dikkate alinmaz
' ("06 ABC 123" = "06abc123").

Option Explicit

Private Const KAYNAK_SAYFA As String = "Sayfa4"
Private Const KAYNAK_SUTUN As String = "H"
Private Const HEDEF_SAYFA As String = "emn"
Private Const HEDEF_SUTUN As String = "F"
Private Const SONUC_SUTUN As String = "S"
Private Const BASLIK_SATIRI As Long = 1   ' Baslik yoksa 0 yapin

Public Sub PlakalariKarsilastir()
    Dim wsKaynak As Worksheet, wsHedef As Worksheet
    Dim plakalar As Object
    Dim sonSatir As Long, i As Long, ilkSatir As Long
    Dim anahtar As String, eslesen As Long

    Set wsKaynak = ThisWorkbook.Worksheets(KAYNAK_SAYFA)
    Set wsHedef = ThisWorkbook.Worksheets(HEDEF_SAYFA)
    ilkSatir = BASLIK_SATIRI + 1

    ' emn sayfasindaki plakalari sozluge al
    Set plakalar = CreateObject("Scripting.Dictionary")
    sonSatir = wsHedef.Cells(wsHedef.Rows.Count, HEDEF_SUTUN).End(xlUp).Row
    For i = ilkSatir To sonSatir
        anahtar = Normallestir(wsHedef.Cells(i, HEDEF_SUTUN).Value)
        If Len(anahtar) > 0 Then plakalar(anahtar) = True
    Next i

    Application.ScreenUpdating = False

    ' S sutununu temizle
    sonSatir = wsKaynak.Cells(wsKaynak.Rows.Count, KAYNAK_SUTUN).End(xlUp).Row
    With wsKaynak.Range(wsKaynak.Cells(ilkSatir, SONUC_SUTUN), wsKaynak.Cells(Application.Max(sonSatir, ilkSatir), SONUC_SUTUN))
        .ClearContents
        .Interior.ColorIndex = xlNone
        .Font.ColorIndex = xlAutomatic
        .Font.Bold = False
    End With

    If BASLIK_SATIRI > 0 Then
        wsKaynak.Cells(BASLIK_SATIRI, SONUC_SUTUN).Value = "Eslesen Plaka"
        wsKaynak.Cells(BASLIK_SATIRI, SONUC_SUTUN).Font.Bold = True
    End If

    ' Sayfa4 plakalarini kontrol et, eslesenleri S sutununa renkli yaz
    For i = ilkSatir To sonSatir
        anahtar = Normallestir(wsKaynak.Cells(i, KAYNAK_SUTUN).Value)
        If Len(anahtar) > 0 Then
            If plakalar.Exists(anahtar) Then
                With wsKaynak.Cells(i, SONUC_SUTUN)
                    .Value = wsKaynak.Cells(i, KAYNAK_SUTUN).Value
                    .Interior.Color = RGB(198, 239, 206)   ' acik yesil zemin
                    .Font.Color = RGB(0, 97, 0)            ' koyu yesil yazi
                    .Font.Bold = True
                End With
                eslesen = eslesen + 1
            End If
        End If
    Next i

    wsKaynak.Columns(SONUC_SUTUN).AutoFit
    Application.ScreenUpdating = True

    MsgBox eslesen & " adet eslesen plaka bulundu ve " & SONUC_SUTUN & " sutununa yazildi.", vbInformation
End Sub

Private Function Normallestir(ByVal deger As Variant) As String
    Dim s As String
    If IsError(deger) Then Exit Function
    s = UCase$(Trim$(CStr(deger)))
    s = Replace(s, " ", "")
    s = Replace(s, Chr(160), "")   ' bolunmez bosluk
    s = Replace(s, "-", "")
    Normallestir = s
End Function
