Attribute VB_Name = "EmnTekrarSil"
' emn sayfasinda F sutununda tekrar eden plakalari bulur.
' Her plaka icin G sutunundaki tarihi (YYYYAA, ornek 201710 = Ekim 2017)
' en kucuk olan satiri birakir, diger tekrar eden satirlari siler.
' Yil ayniysa ayi once olan kalir. Tarih de ayniysa ilk satir kalir.
' Satirlarin sirasi degismez.

Option Explicit

Private Const SAYFA_ADI As String = "emn"
Private Const PLAKA_SUTUNU As String = "F"
Private Const TARIH_SUTUNU As String = "G"
Private Const BASLIK_SATIRI As Long = 1   ' Baslik yoksa 0 yapin

Public Sub TekrarEdenPlakalariSil()
    Dim ws As Worksheet
    Dim enIyiSatir As Object, enIyiTarih As Object
    Dim ilkSatir As Long, sonSatir As Long, i As Long
    Dim plaka As String, tarih As Double
    Dim silinecek As Range, silinenSayi As Long

    Set ws = ThisWorkbook.Worksheets(SAYFA_ADI)
    ilkSatir = BASLIK_SATIRI + 1
    sonSatir = ws.Cells(ws.Rows.Count, PLAKA_SUTUNU).End(xlUp).Row
    If sonSatir < ilkSatir Then
        MsgBox "emn sayfasinda veri bulunamadi.", vbExclamation
        Exit Sub
    End If

    ' Her plaka icin en kucuk tarihli satiri bul
    Set enIyiSatir = CreateObject("Scripting.Dictionary")
    Set enIyiTarih = CreateObject("Scripting.Dictionary")
    For i = ilkSatir To sonSatir
        plaka = PlakaAnahtari(ws.Cells(i, PLAKA_SUTUNU).Value)
        If Len(plaka) > 0 Then
            tarih = TarihDegeri(ws.Cells(i, TARIH_SUTUNU).Value)
            If Not enIyiSatir.Exists(plaka) Then
                enIyiSatir(plaka) = i
                enIyiTarih(plaka) = tarih
            ElseIf tarih < enIyiTarih(plaka) Then
                enIyiSatir(plaka) = i
                enIyiTarih(plaka) = tarih
            End If
        End If
    Next i

    ' Silinecek satir sayisini hesapla ve onay al
    For i = ilkSatir To sonSatir
        plaka = PlakaAnahtari(ws.Cells(i, PLAKA_SUTUNU).Value)
        If Len(plaka) > 0 Then
            If enIyiSatir(plaka) <> i Then silinenSayi = silinenSayi + 1
        End If
    Next i

    If silinenSayi = 0 Then
        MsgBox "Tekrar eden plaka bulunamadi.", vbInformation
        Exit Sub
    End If

    If MsgBox(silinenSayi & " adet tekrar eden satir silinecek." & vbCrLf & _
              "Bu islem geri alinamaz. Dosyanin yedegini aldiniz mi?" & vbCrLf & vbCrLf & _
              "Devam edilsin mi?", vbYesNo + vbQuestion, "Tekrar Eden Plakalar") <> vbYes Then Exit Sub

    Application.ScreenUpdating = False
    Application.Calculation = xlCalculationManual

    ' Alttan yukari dogru sil (ust satir numaralari bozulmasin)
    For i = sonSatir To ilkSatir Step -1
        plaka = PlakaAnahtari(ws.Cells(i, PLAKA_SUTUNU).Value)
        If Len(plaka) > 0 Then
            If enIyiSatir(plaka) <> i Then
                If silinecek Is Nothing Then
                    Set silinecek = ws.Rows(i)
                Else
                    Set silinecek = Union(silinecek, ws.Rows(i))
                End If
                ' Cok parcali araliklar yavaslar, parca parca sil
                If silinecek.Areas.Count >= 500 Then
                    silinecek.Delete
                    Set silinecek = Nothing
                End If
            End If
        End If
    Next i
    If Not silinecek Is Nothing Then silinecek.Delete

    Application.Calculation = xlCalculationAutomatic
    Application.ScreenUpdating = True

    MsgBox silinenSayi & " adet tekrar eden satir silindi." & vbCrLf & _
           enIyiSatir.Count & " farkli plaka kaldi.", vbInformation
End Sub

Private Function PlakaAnahtari(ByVal deger As Variant) As String
    If IsError(deger) Then Exit Function
    PlakaAnahtari = UCase$(Trim$(Replace(CStr(deger), Chr(160), " ")))
End Function

' G sutunundaki 201710 gibi degeri sayiya cevirir (metin ya da sayi olabilir).
' Bos veya gecersiz tarih en buyuk kabul edilir, boylece gecerli tarihli satir kalir.
Private Function TarihDegeri(ByVal deger As Variant) As Double
    Dim s As String
    If IsError(deger) Then
        TarihDegeri = 999999999
        Exit Function
    End If
    s = Trim$(CStr(deger))
    If Len(s) = 0 Or Not IsNumeric(s) Then
        TarihDegeri = 999999999
    Else
        TarihDegeri = CDbl(s)
    End If
End Function
