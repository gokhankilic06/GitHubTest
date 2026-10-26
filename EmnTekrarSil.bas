Attribute VB_Name = "EmnTekrarSil"
' emn sayfasinda F sutununda tekrar eden plakalari bulur.
' Her plaka icin G sutunundaki tarihi (YYYYAA, ornek 201710 = Ekim 2017)
' en kucuk olan satiri birakir, diger tekrar eden satirlari siler.
' Yil ayniysa ayi once olan kalir. Tarih de ayniysa ustteki satir kalir.
' Satirlarin sirasi degismez. Yalnizca emn sayfasinda calisir.

Option Explicit

Private Const SAYFA_ADI As String = "emn"
Private Const PLAKA_SUTUNU As String = "F"
Private Const TARIH_SUTUNU As String = "G"
Private Const BASLIK_SATIRI As Long = 1   ' Baslik yoksa 0 yapin
Private Const GECERSIZ_TARIH As Double = 999999999

Public Sub TekrarEdenPlakalariSil()
    Dim ws As Worksheet
    Dim enIyiSatir As Object, enIyiTarih As Object
    Dim ilkSatir As Long, sonSatir As Long, i As Long
    Dim plaka As String, tarih As Double
    Dim silinenSayi As Long, gecersizSayi As Long
    Dim blokSon As Long, sil As Boolean, mesaj As String

    Set ws = SayfaBul(SAYFA_ADI)
    If ws Is Nothing Then
        MsgBox "'" & SAYFA_ADI & "' adinda bir sayfa bulunamadi.", vbExclamation
        Exit Sub
    End If
    If ws.ProtectContents Then
        MsgBox "'" & ws.Name & "' sayfasi korumali. Once korumayi kaldirin.", vbExclamation
        Exit Sub
    End If

    ' Filtre aciksa gizli satirlar atlanmasin diye tum satirlari goster
    If ws.FilterMode Then ws.ShowAllData

    ilkSatir = BASLIK_SATIRI + 1
    sonSatir = ws.Cells(ws.Rows.Count, PLAKA_SUTUNU).End(xlUp).Row
    If sonSatir < ilkSatir Then
        MsgBox "'" & ws.Name & "' sayfasinda veri bulunamadi.", vbExclamation
        Exit Sub
    End If

    ' Her plaka icin en kucuk tarihli satiri bul
    Set enIyiSatir = CreateObject("Scripting.Dictionary")
    Set enIyiTarih = CreateObject("Scripting.Dictionary")
    For i = ilkSatir To sonSatir
        plaka = PlakaAnahtari(ws.Cells(i, PLAKA_SUTUNU).Value)
        If Len(plaka) > 0 Then
            tarih = TarihDegeri(ws.Cells(i, TARIH_SUTUNU).Value)
            If tarih = GECERSIZ_TARIH Then gecersizSayi = gecersizSayi + 1
            If Not enIyiSatir.Exists(plaka) Then
                enIyiSatir.Add plaka, i
                enIyiTarih.Add plaka, tarih
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

    mesaj = silinenSayi & " adet tekrar eden satir silinecek." & vbCrLf & _
            enIyiSatir.Count & " farkli plaka kalacak."
    If gecersizSayi > 0 Then
        mesaj = mesaj & vbCrLf & vbCrLf & "Uyari: " & gecersizSayi & _
                " satirda G sutunundaki tarih bos veya okunamadi. " & _
                "Bu satirlar en yeni tarihli sayilir (gecerli tarihli satir kalir)."
    End If
    mesaj = mesaj & vbCrLf & vbCrLf & "Bu islem geri alinamaz. Dosyanin yedegini aldiniz mi?" & _
            vbCrLf & "Devam edilsin mi?"
    If MsgBox(mesaj, vbYesNo + vbQuestion, "Tekrar Eden Plakalar") <> vbYes Then Exit Sub

    On Error GoTo Hata
    Application.ScreenUpdating = False
    Application.Calculation = xlCalculationManual

    ' Alttan yukari dogru sil (ust satir numaralari bozulmasin).
    ' Art arda gelen silinecek satirlar tek seferde silinir.
    blokSon = 0
    For i = sonSatir To ilkSatir Step -1
        sil = False
        plaka = PlakaAnahtari(ws.Cells(i, PLAKA_SUTUNU).Value)
        If Len(plaka) > 0 Then sil = (enIyiSatir(plaka) <> i)
        If sil Then
            If blokSon = 0 Then blokSon = i
        ElseIf blokSon > 0 Then
            ws.Rows((i + 1) & ":" & blokSon).Delete
            blokSon = 0
        End If
    Next i
    If blokSon > 0 Then ws.Rows(ilkSatir & ":" & blokSon).Delete

    Application.Calculation = xlCalculationAutomatic
    Application.ScreenUpdating = True

    MsgBox silinenSayi & " adet tekrar eden satir silindi." & vbCrLf & _
           enIyiSatir.Count & " farkli plaka kaldi.", vbInformation
    Exit Sub

Hata:
    Application.Calculation = xlCalculationAutomatic
    Application.ScreenUpdating = True
    MsgBox "Silme sirasinda hata olustu: " & Err.Description & vbCrLf & _
           "Dosyayi kaydetmeden kapatip yedekten devam edin.", vbCritical
End Sub

' Sayfa adini buyuk/kucuk harf ve bas/son bosluklarina bakmadan bulur
Private Function SayfaBul(ByVal ad As String) As Worksheet
    Dim ws As Worksheet
    For Each ws In ActiveWorkbook.Worksheets
        If StrComp(Trim$(ws.Name), Trim$(ad), vbTextCompare) = 0 Then
            Set SayfaBul = ws
            Exit Function
        End If
    Next ws
End Function

Private Function PlakaAnahtari(ByVal deger As Variant) As String
    If IsError(deger) Then Exit Function
    PlakaAnahtari = UCase$(Trim$(Replace(CStr(deger), Chr(160), " ")))
End Function

' G sutunundaki degeri YYYYAA sayisina cevirir.
' Kabul edilenler: 201710 (sayi veya metin) ya da gercek Excel tarihi.
' Bos veya okunamayan deger en buyuk kabul edilir, boylece gecerli tarihli satir kalir.
Private Function TarihDegeri(ByVal deger As Variant) As Double
    Dim s As String, rakamlar As String, k As Long, ch As String

    TarihDegeri = GECERSIZ_TARIH
    If IsError(deger) Or IsEmpty(deger) Then Exit Function

    ' Hucrede gercek tarih varsa (ornek 01.10.2017) yil*100 + ay kullan
    If VarType(deger) = vbDate Then
        TarihDegeri = Year(deger) * 100 + Month(deger)
        Exit Function
    End If

    ' Sayi veya metin: yalnizca rakamlari al, ilk 6 rakam YYYYAA
    s = Trim$(CStr(deger))
    For k = 1 To Len(s)
        ch = Mid$(s, k, 1)
        If ch >= "0" And ch <= "9" Then
            rakamlar = rakamlar & ch
        ElseIf Len(rakamlar) > 0 Then
            Exit For
        End If
    Next k
    If Len(rakamlar) >= 6 Then TarihDegeri = CDbl(Left$(rakamlar, 6))
End Function
