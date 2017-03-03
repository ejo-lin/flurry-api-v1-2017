Imports System.Net
Imports System.IO
Imports Newtonsoft.Json.Linq
Imports System.Text.RegularExpressions


Class DataChip
    Public Country As String
    Public Value As Integer
    Public RecDate As Date

End Class

Module Parser


    Public CLOCK_TIMER As DateTime = DateTime.Now
    Public PF_API_ACCESS_CODE As String = System.Configuration.ConfigurationManager.AppSettings("PF_API_ACCESS_CODE") 
    Public IMF_API_ACCESS_CODE As String = System.Configuration.ConfigurationManager.AppSettings("IMF_API_ACCESS_CODE")
    Public CL_API_ACCESS_CODE As String = System.Configuration.ConfigurationManager.AppSettings("CL_API_ACCESS_CODE")

    Sub ShowTimer(txt As String)
        Exit Sub
        Dim cur As DateTime = DateTime.Now
        System.Console.Write(txt)
        System.Console.Write(" costs ")
        System.Console.Write(cur.Ticks - CLOCK_TIMER.Ticks)
        System.Console.WriteLine(" Ticks")
        CLOCK_TIMER = DateTime.Now
    End Sub

    Public countryMap As New Hashtable
    Sub Main(ByVal agv As String())

        ' build countryMapping
        countryMap = Get_CountryTable()


        Dim onlySyncDB As Boolean = False
        Dim needCreateCognosCube As Boolean = False

        Dim parseSpecMetriconly As Boolean = False
        Dim SpecMetricName As String = ""

        Dim StartDate As DateTime = DateTime.Today
        Dim EndDate As DateTime = DateTime.Today

        Dim Duration As Integer = 20 ' default:20    344 (12/12)

        If agv.Length > 0 Then

            For Each agvItem As String In agv
                If agvItem.ToUpper.StartsWith("METRIC=") = True Then
                    'parseSpecMetriconly = True
                    'SpecMetricName = agvItem.Substring(7)
                ElseIf agvItem.ToUpper.StartsWith("DURATION=") = True Then
                    Duration = Integer.Parse(agvItem.Substring(9))
                ElseIf agvItem.ToUpper.StartsWith("SYNCDBONLY") = True Then
                    onlySyncDB = True
                ElseIf agvItem.ToUpper.StartsWith("SYNCCOGNOS") = True Then
                    needCreateCognosCube = True
                ElseIf agvItem.ToUpper.StartsWith("PERIOD=") = True Then
                    Dim dateTxt As String = agvItem.Substring(7)

                    Dim regex1 As Regex = New Regex("\d\d\d\d-\d\d")
                    Dim match1 As Match = regex1.Match(dateTxt)

                    If match1.Success Then
                        StartDate = Date.Parse(dateTxt + "-01")
                        EndDate = StartDate.AddMonths(1).AddDays(-1)
                    End If

                    Dim regex2 As Regex = New Regex("\d\d\d\d-\d\d-\d\d")
                    Dim match2 As Match = regex2.Match(dateTxt)

                    If match2.Success Then
                        StartDate = Date.Parse(dateTxt)
                        EndDate = StartDate
                    End If

                End If

            Next

        End If

        ' daily parse yeaterday and last 5
        If EndDate = StartDate Then
            StartDate = EndDate.AddDays(-Duration)
        End If

        Dim isV1 As Boolean = True

        If isV1 Then
            ' new API
            PF_API_ACCESS_CODE = System.Configuration.ConfigurationManager.AppSettings("PF_API_ACCESS_CODE_V1")
            IMF_API_ACCESS_CODE = System.Configuration.ConfigurationManager.AppSettings("IMF_API_ACCESS_CODE_V1")
            CL_API_ACCESS_CODE = System.Configuration.ConfigurationManager.AppSettings("CL_API_ACCESS_CODE_V1")

            If onlySyncDB = False Then
                Dim PFAppKey As ArrayList = Get_All_Flurry_APP("PF")
                ' daily materic
                For Each parseAppKey As String In PFAppKey
                    ParseDataV1(PF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, "D")
                    ParseDataV1(PF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, "W")
                    ParseDataV1(PF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, "M")
                Next

                Dim CLAppKey As ArrayList = Get_All_Flurry_APP("CL")
                ' daily materic
                For Each parseAppKey As String In CLAppKey
                    ParseDataV1(CL_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, "D")
                    ParseDataV1(CL_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, "W")
                    ParseDataV1(CL_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, "M")
                Next


                Dim IMFAppKey As ArrayList = Get_All_Flurry_APP("IMF")
                ' daily materic
                For Each parseAppKey As String In IMFAppKey

                    ParseDataV1(IMF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, "D")
                    ParseDataV1(IMF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, "W")
                    ParseDataV1(IMF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, "M")
                Next
            End If
        Else
            ' Reporting API Shutdown: March 6, 2017
            Dim DefaultMetricAry As String() = {"ActiveUsers", "ActiveUsersByWeek", "ActiveUsersByMonth", "NewUsers", "Sessions", "RetainedUsers", "AvgPageViewsPerSession"}

            Dim DailyMetricAry As String() = {"ActiveUsers", "NewUsers", "Sessions", "RetainedUsers"}
            Dim WeeklyMetricAry As String() = {"ActiveUsersByWeek", "NewUsers", "Sessions", "RetainedUsers"}
            Dim MonthlyMetricAry As String() = {"ActiveUsersByMonth", "NewUsers", "Sessions", "RetainedUsers"}

            Dim ParseTargetMetric As New ArrayList

            If onlySyncDB = False Then
                Dim PFAppKey As ArrayList = Get_All_Flurry_APP("PF")
                ' daily materic
                For Each parseMetric As String In DailyMetricAry
                    For Each parseAppKey As String In PFAppKey
                        ParaseData(PF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, parseMetric, "DAYS")

                    Next
                Next
                ' weekly materic
                For Each parseMetric As String In WeeklyMetricAry
                    For Each parseAppKey As String In PFAppKey
                        ParaseData(PF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, parseMetric, "WEEKS")
                    Next
                Next
                ' monthly materic
                For Each parseMetric As String In MonthlyMetricAry
                    For Each parseAppKey As String In PFAppKey
                        ParaseData(PF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, parseMetric, "MONTHS")
                    Next
                Next


                Dim CLAppKey As ArrayList = Get_All_Flurry_APP("CL")
                ' daily materic
                For Each parseMetric As String In DailyMetricAry
                    For Each parseAppKey As String In CLAppKey
                        ParaseData(CL_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, parseMetric, "DAYS")
                    Next
                Next
                ' weekly materic
                For Each parseMetric As String In WeeklyMetricAry
                    For Each parseAppKey As String In CLAppKey
                        ParaseData(CL_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, parseMetric, "WEEKS")
                    Next
                Next
                ' monthly materic
                For Each parseMetric As String In MonthlyMetricAry
                    For Each parseAppKey As String In CLAppKey
                        ParaseData(CL_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, parseMetric, "MONTHS")
                    Next
                Next



                Dim IMFAppKey As ArrayList = Get_All_Flurry_APP("IMF")
                ' daily materic
                For Each parseMetric As String In DailyMetricAry
                    For Each parseAppKey As String In IMFAppKey
                        ParaseData(IMF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, parseMetric, "DAYS")
                    Next
                Next
                ' weekly materic
                For Each parseMetric As String In WeeklyMetricAry
                    For Each parseAppKey As String In IMFAppKey
                        ParaseData(IMF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, parseMetric, "WEEKS")
                    Next
                Next
                ' monthly materic
                For Each parseMetric As String In MonthlyMetricAry
                    For Each parseAppKey As String In IMFAppKey
                        ParaseData(IMF_API_ACCESS_CODE, parseAppKey, StartDate, EndDate, parseMetric, "MONTHS")
                    Next
                Next

            End If

        End If

    End Sub

    Sub SendMail(ByVal mailTo As String, ByVal subject As String, ByVal mailBody As String)
        Dim SMTPServer As System.Net.Mail.SmtpClient = New System.Net.Mail.SmtpClient("127.0.0.1")

        Dim MailMsg = New System.Net.Mail.MailMessage
        MailMsg.IsBodyHtml = True
        MailMsg.From = New Mail.MailAddress("xxx@cyberlink.com", "xxx")
        Dim mailtoArray As String() = mailTo.Split(",")
        For Each user As String In mailtoArray
            MailMsg.To.Add(New Mail.MailAddress(user))
        Next
        MailMsg.Subject = subject
        MailMsg.Body = mailBody
        SMTPServer.Send(MailMsg)

        MailMsg = Nothing
        SMTPServer = Nothing
    End Sub

    Sub ParseDataV1(CLCode As String, APPKey As String, StartDate As Date, EndDate As Date, Groupby As String)
        Dim groupbyTxt As String = "day"
        If Groupby = "D" Then
            groupbyTxt = "day"
        ElseIf Groupby = "M" Then
            groupbyTxt = "month"
        ElseIf Groupby = "W" Then
            groupbyTxt = "week"
        End If

        Dim queryStartDate = StartDate.ToString("yyyy-MM-dd")
        Dim queryEndDate = EndDate.ToString("yyyy-MM-dd")

        If Groupby = "D" Then
            ' no change
        ElseIf Groupby = "M" Then
            ' by month, must be start w/ 1st day and end w/ 1st day of next month
            queryStartDate = Date.Parse(CStr(StartDate.Year) + "-" + CStr(StartDate.Month) + "-01").ToString("yyyy-MM-dd")
            queryEndDate = Date.Parse(CStr(EndDate.Year) + "-" + CStr(EndDate.Month) + "-01").AddMonths(1).ToString("yyyy-MM-dd")
        ElseIf Groupby = "W" Then
            ' Week must start on a Monday and end on a Monday
            queryStartDate = StartDate.AddDays(1 - CInt(StartDate.DayOfWeek)).ToString("yyyy-MM-dd")
            queryEndDate = EndDate.AddDays((7 + 1 - CInt(EndDate.DayOfWeek)) Mod 7).ToString("yyyy-MM-dd")
        End If


        Dim URL As String = "https://api-metrics.flurry.com/public/v1/data/appUsage/" + groupbyTxt + "/app/country?metrics=sessions,activeDevices,newDevices&" + _
                    "filters=app|apiKey-in[" + APPKey + "]&" + _
                    "dateTime=" + queryStartDate + "/" + queryEndDate

        ShowTimer("Start")

        Dim httpReq As WebRequest = WebRequest.Create(URL)
        httpReq.Method = "GET"
        httpReq.Headers.Add("Authorization", "Bearer " + CLCode)

        Dim httpRes As WebResponse
        Try
            httpRes = httpReq.GetResponse()
        Catch ex As Exception
            Threading.Thread.Sleep(10000)
            httpReq = WebRequest.Create(URL)
            httpReq.Method = "GET"
            httpReq.Headers.Add("Authorization", "Bearer " + CLCode)
            httpRes = httpReq.GetResponse()
        End Try


        Dim dataStream = httpRes.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim responseFromServer As String = reader.ReadToEnd()

        reader.Close()
        dataStream.Close()
        httpRes.Close()

        ShowTimer("GetHTML")

        Dim FluzzyData As JObject = JObject.Parse(responseFromServer)

        ShowTimer("Get Root Object")

        Dim sql As New ECL.Utils.SQLWrapper.SQLWrapper()

        Dim DataRows As JArray = FluzzyData("rows")

        Dim MetricName_Session As String = "Sessions"
        Dim MetricName_NewUsers As String = "NewUsers"
        Dim MetricName_ActiveUsers As String = "ActiveUsers"
        Dim MetricName_RetainedUsers As String = "RetainedUsers"

        For Each row As JObject In DataRows

            Dim dc As New DataChip
            Dim countryText As String = countryMap.Item(row.Item("country|name").ToString)
            If countryText Is Nothing Then
                ' send mail to system op
                Dim mail_list As String = System.Configuration.ConfigurationManager.AppSettings("SYSOP_MAIL_LIST")
                SendMail(mail_list, "[Flurry] counrey name is not existed", "can not find code of '" + row.Item("country|name").ToString + "'")
            Else
                ' generate the same data like previous version
                If Groupby = "D" Then
                    MetricName_ActiveUsers = "ActiveUsers"
                ElseIf Groupby = "M" Then
                    MetricName_ActiveUsers = "ActiveUsersByMonth"
                ElseIf Groupby = "W" Then
                    MetricName_ActiveUsers = "ActiveUsersByWeek"
                End If

                dc.Country = countryText
                dc.RecDate = row.Item("dateTime")
                dc.Value = row.Item("sessions")
                Flurry_Data_InsertDB(sql, dc, APPKey, MetricName_Session, Groupby)

                dc.Value = row.Item("activeDevices")
                Flurry_Data_InsertDB(sql, dc, APPKey, MetricName_ActiveUsers, Groupby)

                dc.Value = row.Item("newDevices")
                Flurry_Data_InsertDB(sql, dc, APPKey, MetricName_NewUsers, Groupby)

                ' for Retained users data
                'dc.Value = row.Item("??")
                'Flurry_Data_InsertDB(sql, dc, APPKey, MetricName_RetainedUsers, Groupby)

            End If
        Next

        BatchInsertDB(sql)

        System.Console.Write(" ")
        System.Console.Write(rowCount)
        System.Console.WriteLine(" --- DONE")
        rowCount = 0
        sql.close()

        'System.Console.ReadKey()


    End Sub

    Sub ParaseData(CLCode As String, APPKey As String, StartDate As Date, EndDate As Date, Metric As String, Groupby As String)
        ' DataType { D, M, W}
        System.Console.Write(Metric + " of " + APPKey + " from " + StartDate.ToString("yyyy/MM/dd") + "~" + EndDate.ToString("yyyy/MM/dd") + " ")

        Dim URL As String = "http://api.flurry.com/appMetrics/" + Metric + "?" + _
                            "apiAccessCode=" + CLCode + "&" + _
                            "apiKey=" + APPKey + "&" + _
                            "startDate=" + StartDate.ToString("yyyy-MM-dd") + "&" + _
                            "endDate=" + EndDate.ToString("yyyy-MM-dd") + "&" + _
                            "country=ALL" + "&groupBy=" + Groupby

        ShowTimer("Start")

        Dim httpReq As WebRequest = WebRequest.Create(URL)
        httpReq.Method = "GET"
        Dim httpRes As WebResponse
        Try
            httpRes = httpReq.GetResponse()
        Catch ex As Exception
            Threading.Thread.Sleep(10000)
            httpReq = WebRequest.Create(URL)
            httpReq.Method = "GET"
            httpRes = httpReq.GetResponse()
        End Try


        Dim dataStream = httpRes.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim responseFromServer As String = reader.ReadToEnd()

        reader.Close()
        dataStream.Close()
        httpRes.Close()

        ShowTimer("GetHTML")

        Dim FluzzyData As JObject = JObject.Parse(responseFromServer)

        ShowTimer("Get Root Object")

        Dim dcAry As New ArrayList

        Dim num_count_country As Integer = 0
        If TypeOf FluzzyData("country") Is JArray Then
            num_count_country = 2
        Else
            If Not FluzzyData.GetValue("country") Is Nothing Then
                num_count_country = 1
            Else
                num_count_country = 0
            End If

        End If

        ' to slow down the connenction w/ Flurry. Flurry asks can not over 1 reqeust / per second
        Threading.Thread.Sleep(1000)

        If num_count_country = 0 Then
            Exit Sub
        End If

        Dim sql As New ECL.Utils.SQLWrapper.SQLWrapper()

        If num_count_country > 1 Then
            ShowTimer("Another")
            Dim items As JArray = FluzzyData("country")

            ShowTimer("Parse Object")
            For Each item As JObject In CType(items, JArray)
                parseCountryItem(item, sql, APPKey, Metric, Left(Groupby, 1))
            Next

        Else
            ShowTimer("Another")
            Dim item As JObject = FluzzyData("country")
            ShowTimer("Parse Object")
            parseCountryItem(item, sql, APPKey, Metric, Left(Groupby, 1))

        End If

        BatchInsertDB(sql)

        System.Console.Write(" ")
        System.Console.Write(rowCount)
        System.Console.WriteLine(" --- DONE")
        rowCount = 0
        sql.close()

        'System.Console.ReadKey()
    End Sub

    Sub ParaseDataNoCountry(CLCode As String, APPKey As String, StartDate As Date, EndDate As Date, Metric As String, Groupby As String)
        ' DataType { D, M, W}
        System.Console.Write(Metric + " of " + APPKey + " from " + StartDate.ToString("yyyy/MM/dd") + "~" + EndDate.ToString("yyyy/MM/dd") + " w/o country ")

        Dim URL As String = "http://api.flurry.com/appMetrics/" + Metric + "?" + _
                            "apiAccessCode=" + CLCode + "&" + _
                            "apiKey=" + APPKey + "&" + _
                            "startDate=" + StartDate.ToString("yyyy-MM-dd") + "&" + _
                            "endDate=" + EndDate.ToString("yyyy-MM-dd") + "&" + _
                            "groupBy=" + Groupby

        ShowTimer("Start")

        Dim httpReq As WebRequest = WebRequest.Create(URL)
        httpReq.Method = "GET"
        Dim httpRes As WebResponse
        Try
            httpRes = httpReq.GetResponse()
        Catch ex As Exception
            Threading.Thread.Sleep(10000)
            httpReq = WebRequest.Create(URL)
            httpReq.Method = "GET"
            httpRes = httpReq.GetResponse()
        End Try


        Dim dataStream = httpRes.GetResponseStream()
        Dim reader As New StreamReader(dataStream)
        Dim responseFromServer As String = reader.ReadToEnd()

        reader.Close()
        dataStream.Close()
        httpRes.Close()

        ShowTimer("GetHTML")

        Dim FluzzyData As JObject = JObject.Parse(responseFromServer)

        ShowTimer("Get Root Object")

        Dim dcAry As New ArrayList

        ' to slow down the connenction w/ Flurry. Flurry asks can not over 1 reqeust / per second
        Threading.Thread.Sleep(1000)

        Dim sql As New ECL.Utils.SQLWrapper.SQLWrapper

        parseNoCountryItem(FluzzyData, sql, APPKey, Metric, Left(Groupby, 1))

        BatchInsertDB(sql)

        System.Console.Write(" ")
        System.Console.Write(rowCount)
        System.Console.WriteLine(" --- DONE")
        rowCount = 0
        sql.close()

    End Sub

    Sub parseNoCountryItem(item As JObject, sql As ECL.Utils.SQLWrapper.SQLWrapper, APPKey As String, Metric As String, gropuby As String)
        If item Is Nothing Then
            Exit Sub
        End If

        ShowTimer("parseNoCountryItem")

        Dim dayObj As JToken = item("day")
        ShowTimer("day JToken")

        If Not dayObj Is Nothing Then
            If TypeOf item("day") Is JArray Then
                Dim dayAry As JArray = item("day")
                ShowTimer("dayAry JArray")
                For Each dayItem As JObject In dayAry
                    Dim dc As New DataChip
                    ShowTimer("get attribute")
                    dc.Country = "ALL"
                    ShowTimer("get Country")
                    dc.RecDate = Date.Parse(dayItem.Value(Of String)("@date"))
                    ShowTimer("get RecDate")
                    dc.Value = dayItem.Value(Of Integer)("@value")
                    ShowTimer("get Value")
                    Flurry_Data_InsertDB(sql, dc, APPKey, Metric, gropuby)
                    ShowTimer("insert databse")
                Next
            Else
                ' no [  ], only one value
                Dim dc As New DataChip
                ShowTimer("get attribute")
                dc.Country = "ALL"
                ShowTimer("get Country")
                'dc.RecDate = Date.Parse(item.Value(Of String)("day.@date"))
                'dc.Value = item.Value(Of Integer)("day.@value")
                dc.RecDate = dayObj.Value(Of String)("@date")
                ShowTimer("get RecDate")
                dc.Value = dayObj.Value(Of String)("@value")
                ShowTimer("get Value")
                Flurry_Data_InsertDB(sql, dc, APPKey, Metric, gropuby)
                ShowTimer("insert databse")
            End If

        End If

    End Sub

    Sub parseCountryItem(item As JObject, sql As ECL.Utils.SQLWrapper.SQLWrapper, APPKey As String, Metric As String, gropuby As String)
        If item Is Nothing Then
            Exit Sub
        End If

        ShowTimer("parseCountryItem")

        Dim dayObj As JToken = item("day")
        ShowTimer("day JToken")

        If Not dayObj Is Nothing Then
            If TypeOf item("day") Is JArray Then
                Dim dayAry As JArray = item("day")
                ShowTimer("dayAry JArray")
                For Each dayItem As JObject In dayAry
                    Dim dc As New DataChip
                    ShowTimer("get attribute")
                    dc.Country = item.Value(Of String)("@country")
                    ShowTimer("get Country")
                    dc.RecDate = Date.Parse(dayItem.Value(Of String)("@date"))
                    ShowTimer("get RecDate")
                    dc.Value = dayItem.Value(Of Integer)("@value")
                    ShowTimer("get Value")
                    Flurry_Data_InsertDB(sql, dc, APPKey, Metric, gropuby)
                    ShowTimer("insert databse")
                Next
            Else
                ' no [  ], only one value
                Dim dc As New DataChip
                ShowTimer("get attribute")
                dc.Country = item.Value(Of String)("@country")
                ShowTimer("get Country")
                'dc.RecDate = Date.Parse(item.Value(Of String)("day.@date"))
                'dc.Value = item.Value(Of Integer)("day.@value")
                dc.RecDate = dayObj.Value(Of String)("@date")
                ShowTimer("get RecDate")
                dc.Value = dayObj.Value(Of String)("@value")
                ShowTimer("get Value")
                Flurry_Data_InsertDB(sql, dc, APPKey, Metric, gropuby)
                ShowTimer("insert databse")
            End If

        End If

    End Sub

    Public rowCount As Integer = 0
    Sub Flurry_Data_InsertDB(ByVal sql As ECL.Utils.SQLWrapper.SQLWrapper, chip As DataChip, FlrryID As String, matricName As String, gropuby As String)

        rowCount += 1

        AppendSQLScript(sql, chip, FlrryID, matricName, gropuby)

        ' to improve performance
        'DirectktInsertDB(sql, chip, FlrryID, matricName, gropuby)
        'System.Console.Write(".")

    End Sub

    Sub CreateCube(StartDate As DateTime, EndDate As DateTime, duration As String)
        Dim sql As New ECL.Utils.SQLWrapper.SQLWrapper

        ' 20150723 manually run - by Ejo
        'exec Flurry_Build_Cube '2015/07/03','2015/07/23', 'M'  --- 00:00:34
        'exec Flurry_Build_Cube '2015/07/03','2015/07/23', 'W'  --- 00:00:45
        'exec Flurry_Build_Cube '2015/07/03','2015/07/23', 'D'  --- 00:01:57
        'exec Flurry_Build_Cube '2015/07/03','2015/07/23', 'Q'  --- 00:00:31

        sql.SPCommand = "Flurry_Build_Cube"
        sql.setDateTime("@BegDate", StartDate)
        sql.setDateTime("@EndDate", EndDate)
        sql.setString("@Group", duration)
        sql.execute()

        sql.close()
        'System.Console.Write(".")
        sql = Nothing
    End Sub

    Sub CreateStatisticsCube(StartDate As DateTime, EndDate As DateTime)
        Dim sql As New ECL.Utils.SQLWrapper.SQLWrapper()

        Console.WriteLine(Date.Now)

        System.Console.WriteLine("Flurry_APP_Statistics_Generate  M")
        sql.SPCommand = "Flurry_APP_Statistics_Generate"
        sql.setDateTime("@BegDate", StartDate)
        sql.setDateTime("@EndDate", EndDate)
        sql.setString("@Group", "M")
        sql.execute()

        Console.WriteLine(Date.Now)

        System.Console.WriteLine("Flurry_APP_Statistics_Generate  W")
        sql.SPCommand = "Flurry_APP_Statistics_Generate"
        sql.setDateTime("@BegDate", StartDate)
        sql.setDateTime("@EndDate", EndDate)
        sql.setString("@Group", "W")
        sql.execute()

        Console.WriteLine(Date.Now)

        System.Console.WriteLine("Flurry_APP_Statistics_Generate  D")
        sql.SPCommand = "Flurry_APP_Statistics_Generate"
        sql.setDateTime("@BegDate", StartDate)
        sql.setDateTime("@EndDate", EndDate)
        sql.setString("@Group", "D")
        sql.execute()

        Console.WriteLine(Date.Now)

        sql.close()
        'System.Console.Write(".")
        sql = Nothing
    End Sub
    Sub CreateCognosCube(StartDate As DateTime, EndDate As DateTime)

        Console.WriteLine(Date.Now)

        Dim sql As New ECL.Utils.SQLWrapper.SQLWrapper
        System.Console.WriteLine("Flurry_App_Cognos_Cube_Generate")
        sql.SPCommand = "Flurry_App_Cognos_Cube_Generate"
        sql.setDateTime("@BegDate", StartDate)
        sql.setDateTime("@EndDate", EndDate)
        sql.execute()

        sql.close()
        'System.Console.Write(".")
        sql = Nothing

        Console.WriteLine(Date.Now)

    End Sub

    Sub DirectktInsertDB(ByVal sql As ECL.Utils.SQLWrapper.SQLWrapper, chip As DataChip, FlrryID As String, matricName As String, gropuby As String)

        sql.SPCommand = "Flurry_DATA_Insert"
        sql.setString("@FlurryKey", FlrryID)
        sql.setDateTime("@RecDate", chip.RecDate)
        sql.setString("@MatricName", matricName)
        sql.setString("@Country", chip.Country)
        sql.setInt("@Value", chip.Value)
        sql.setString("@Duration", gropuby)

        sql.execute()

        'System.Console.Write(".")

    End Sub

    Public SQL_script_txt As New Text.StringBuilder
    Dim BATCH_THREAD As Integer = 2000
    Sub AppendSQLScript(ByVal sql As ECL.Utils.SQLWrapper.SQLWrapper, chip As DataChip, FlrryID As String, matricName As String, gropuby As String)

        SQL_script_txt.Append("exec Flurry_DATA_Insert ").Append("'").Append(FlrryID).Append("',").Append("'").Append(chip.RecDate.ToString("yyyy/MM/dd")).Append("',")
        SQL_script_txt.Append("'").Append(matricName).Append("',").Append("'").Append(chip.Country).Append("',")
        SQL_script_txt.Append(chip.Value).Append(",").Append("'").Append(gropuby).Append("';").Append(vbCrLf)
        If rowCount Mod BATCH_THREAD = 0 Then
            ShowTimer("Start writting DB")
            BatchInsertDB(sql)
            ShowTimer("Finish writting DB (" + CStr(BATCH_THREAD) + ")")
        End If
    End Sub

    Sub BatchInsertDB(sql As ECL.Utils.SQLWrapper.SQLWrapper)
        If SQL_script_txt.Length <> 0 Then

            sql.SqlCommand = SQL_script_txt.ToString()
            sql.execute()

            SQL_script_txt.Clear()
        End If
    End Sub


    Function Get_CountryTable() As Hashtable

        Dim keys As New Hashtable
        Dim sql As New ECL.Utils.SQLWrapper.SQLWrapper
        Dim dr As System.Data.SqlClient.SqlDataReader

        sql.SqlCommand = "select * from Flurry_Country_Mapping "

        dr = sql.getDataReader()
        While (dr.Read())
            Try
                keys.Add(dr("CountryName").ToString, dr("CountryCode").ToString)
            Catch ex As Exception
                ' except duplicated data
            End Try
        End While

        dr.Close()
        sql.close()

        Return keys

    End Function

    Function Get_All_Flurry_APP(company As String) As ArrayList

        Dim keys As New ArrayList
        Dim sql As New ECL.Utils.SQLWrapper.SQLWrapper
        Dim dr As System.Data.SqlClient.SqlDataReader

        sql.SqlCommand = "select FlurryKey from  Flurry_APP where Company = @Company"
        ' from 1 to 24
        'sql.SqlCommand = "select FlurryKey from  Flurry_APP where Company = @Company and AppID = 29"
        sql.setString("@Company", company)

        dr = sql.getDataReader()
        While (dr.Read())
            keys.Add(dr("FlurryKey").ToString)
        End While

        dr.Close()
        sql.close()

        Return keys

    End Function

End Module
