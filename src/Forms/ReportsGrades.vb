Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows.Forms

' Reports & Grades screen: sidebar + titlebar (same chrome as HomeForm/Quizzes) plus
' four summary stat cards, a "Score trend" line chart, a "Mastery by topic" bar chart,
' and an "Assessment history" table with status pills — matching the "Reports & Grades"
' screenshot.
Public Class ReportsGrades
    Inherits Form

    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HT_CAPTION As Integer = &H2

    Private ReadOnly userName As String
    Private ReadOnly userRole As String

    Private sidebar As Panel
    Private content As Panel

    ' ----- data -----

    Private Class StatCard
        Public Property Label As String
        Public Property Value As String
        Public Property Note As String
        Public Property NoteColor As Color
    End Class

    ' TODO: the four summary cards need aggregate queries across assessments/
    ' quiz_attempts/lab_sessions plus a notion of "target syllabus size" and
    ' "last term" that isn't modeled in the schema yet — left hard-coded for
    ' now rather than wiring up half-correct numbers. scoreTrend, masteryTopics
    ' and assessments below ARE wired to the database.
    Private ReadOnly stats As New List(Of StatCard) From {
        New StatCard With {.Label = "Overall average", .Value = "85.7%", .Note = "+6.2 vs last term", .NoteColor = Color.FromArgb(120, 220, 170)},
        New StatCard With {.Label = "Experiments completed", .Value = "14 / 20", .Note = "70% of syllabus", .NoteColor = Color.FromArgb(150, 158, 180)},
        New StatCard With {.Label = "Quizzes passed", .Value = "9", .Note = "1 retake pending", .NoteColor = Color.FromArgb(230, 170, 100)},
        New StatCard With {.Label = "Lab hours logged", .Value = "23h 40m", .Note = "Across 31 sessions", .NoteColor = Color.FromArgb(150, 158, 180)}
    }

    ' month, score (0-100) — score trend line chart. Offline fallback shown
    ' immediately; LoadFromDbAsync() (fired from Form.Load) replaces it with
    ' the real rows from `score_trend` for the signed-in user.
    Private scoreTrend As New List(Of (Month As String, Score As Integer)) From {
        ("Mar", 70), ("Apr", 78), ("May", 75), ("Jun", 84), ("Jul", 88)
    }

    ' topic, mastery % — mastery bar chart. Same offline-fallback pattern.
    Private masteryTopics As New List(Of (Topic As String, Percent As Integer)) From {
        ("Acids", 85), ("Solutions", 72), ("Redox", 60), ("Oxides", 78), ("Analysis", 82)
    }

    Private Class AssessmentRow
        Public Property Name As String
        Public Property Type As String
        Public Property DateText As String
        Public Property Score As String
        Public Property Status As String
    End Class

    ' Same offline-fallback pattern as above.
    Private assessments As New List(Of AssessmentRow) From {
        New AssessmentRow With {.Name = "Acid & Base Reaction", .Type = "Practical", .DateText = "12 Jul 2026", .Score = "92%", .Status = "Graded"},
        New AssessmentRow With {.Name = "Precipitation Reaction", .Type = "Practical", .DateText = "18 Jul 2026", .Score = "78%", .Status = "Graded"},
        New AssessmentRow With {.Name = "Titration Quiz", .Type = "Quiz", .DateText = "21 Jul 2026", .Score = "85%", .Status = "Graded"},
        New AssessmentRow With {.Name = "Gas Evolution Report", .Type = "Report", .DateText = "24 Jul 2026", .Score = "—", .Status = "Pending"},
        New AssessmentRow With {.Name = "Flame Test", .Type = "Practical", .DateText = "28 Jul 2026", .Score = "88%", .Status = "Graded"}
    }

    ' Set once the signed-in user is resolved against the database (see
    ' LoadFromDbAsync). Falls back to 1 (the seeded demo user) if that lookup
    ' hasn't happened yet — swap in your real session/user-id management once
    ' you have it instead of relying on this.
    Private currentUserId As Integer = 1

    Public Sub New(displayName As String, role As String)
        userName = If(String.IsNullOrWhiteSpace(displayName), "Student", displayName)
        userRole = If(String.IsNullOrWhiteSpace(role), "Student", role)

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1200, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.AutoScroll = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Reports & Grades"

        BuildTitleBar()
        BuildSidebar()
        BuildContent()

        ' Reflow the dashboard content when the window is maximized/restored so
        ' cards and panels use the available width instead of staying stuck at
        ' the size they were first built at.
        AddHandler Me.Resize, Sub()
                                   If Me.WindowState <> FormWindowState.Minimized Then
                                       BuildContent()
                                   End If
                               End Sub

        AddHandler Me.Load, AddressOf LoadFromDbAsync
    End Sub

    ''' <summary>
    ''' Resolves the signed-in user by email/name, then loads their real score
    ''' trend, topic mastery and assessment history from the database, replacing
    ''' the offline fallback data and re-rendering. Silently keeps the fallback
    ''' if the database isn't reachable.
    ''' </summary>
    Private Async Sub LoadFromDbAsync(sender As Object, e As EventArgs)
        Try
            Dim resolvedId = Await UsersRepository.FindUserIdByDisplayNameAsync(userName)
            If resolvedId.HasValue Then currentUserId = resolvedId.Value

            Dim trendTask = ReportsRepository.GetScoreTrendAsync(currentUserId)
            Dim masteryTask = ReportsRepository.GetMasteryTopicsAsync(currentUserId)
            Dim assessmentsTask = ReportsRepository.GetAssessmentsAsync(currentUserId)
            Await Task.WhenAll(trendTask, masteryTask, assessmentsTask)

            If trendTask.Result.Count > 0 Then scoreTrend = trendTask.Result
            If masteryTask.Result.Count > 0 Then masteryTopics = masteryTask.Result
            If assessmentsTask.Result.Count > 0 Then
                assessments = assessmentsTask.Result.Select(
                    Function(a) New AssessmentRow With {
                        .Name = a.Name, .Type = a.Type, .DateText = a.DateText, .Score = a.Score, .Status = a.Status
                    }).ToList()
            End If

            BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not load reports from database: {ex.Message}")
        End Try
    End Sub

    ' ===================== TITLE BAR =====================

    Private Sub BuildTitleBar()
        Dim titleBar As New Panel()
        titleBar.Dock = DockStyle.Top
        titleBar.Height = 40
        titleBar.BackColor = Color.FromArgb(9, 12, 24)
        Me.Controls.Add(titleBar)

        Dim lblTitle As New Label() With {.Text = "ChemLab Virtual — Reports & Grades", .Font = New Font("Segoe UI", 9.5, FontStyle.Regular),
                                           .ForeColor = Color.FromArgb(150, 158, 185), .AutoSize = True, .Location = New Point(16, 11)}
        titleBar.Controls.Add(lblTitle)

        AddHandler titleBar.MouseDown, Sub(s, e)
                                            If e.Button = MouseButtons.Left Then
                                                ReleaseCapture()
                                                SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
                                            End If
                                        End Sub

        Dim btnClose As New Label() With {.Text = "✕", .Font = New Font("Segoe UI", 10), .Size = New Size(40, 40),
                                           .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = Color.FromArgb(160, 168, 190),
                                           .Cursor = Cursors.Hand, .Dock = DockStyle.Right}
        AddHandler btnClose.Click, Sub() Me.Close()
        AddHandler btnClose.MouseEnter, Sub() btnClose.BackColor = Color.FromArgb(200, 60, 60)
        AddHandler btnClose.MouseLeave, Sub() btnClose.BackColor = Color.Transparent

        Dim btnMax As New Label() With {.Text = "☐", .Font = New Font("Segoe UI", 9), .Size = New Size(40, 40),
                                         .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = Color.FromArgb(160, 168, 190),
                                         .Cursor = Cursors.Hand, .Dock = DockStyle.Right}
        AddHandler btnMax.Click, Sub() Me.WindowState = If(Me.WindowState = FormWindowState.Maximized, FormWindowState.Normal, FormWindowState.Maximized)
        AddHandler btnMax.MouseEnter, Sub() btnMax.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMax.MouseLeave, Sub() btnMax.BackColor = Color.Transparent

        Dim btnMin As New Label() With {.Text = "—", .Font = New Font("Segoe UI", 9), .Size = New Size(40, 40),
                                         .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = Color.FromArgb(160, 168, 190),
                                         .Cursor = Cursors.Hand, .Dock = DockStyle.Right}
        AddHandler btnMin.Click, Sub() Me.WindowState = FormWindowState.Minimized
        AddHandler btnMin.MouseEnter, Sub() btnMin.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMin.MouseLeave, Sub() btnMin.BackColor = Color.Transparent

        titleBar.Controls.Add(btnClose)
        titleBar.Controls.Add(btnMax)
        titleBar.Controls.Add(btnMin)
    End Sub

    ' ===================== SIDEBAR (same nav as HomeForm/Quizzes, "Reports & Grades" active) =====================

    Private Sub BuildSidebar()
        sidebar = New Panel()
        sidebar.Dock = DockStyle.Left
        sidebar.Width = 244
        sidebar.BackColor = Color.FromArgb(12, 15, 30)
        Me.Controls.Add(sidebar)

        Dim iconBox As New Panel()
        iconBox.Size = New Size(44, 44)
        iconBox.Location = New Point(20, 20)
        AddHandler iconBox.Paint, AddressOf PaintLogoIcon
        sidebar.Controls.Add(iconBox)

        Dim lblChem As New Label() With {.Text = "ChemLab", .Font = New Font("Segoe UI", 13, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(74, 20)}
        Dim lblVirtual As New Label() With {.Text = "V I R T U A L", .Font = New Font("Segoe UI", 7.5), .ForeColor = Color.FromArgb(150, 158, 185), .AutoSize = True, .Location = New Point(75, 43)}
        sidebar.Controls.Add(lblChem)
        sidebar.Controls.Add(lblVirtual)

        Dim navItems As (String, String, Boolean)() = {
            ("home", "Home", False),
            ("flask", "Lab Workspace", False),
            ("book", "Experiments", False),
            ("grid", "Apparatus", False),
            ("beaker", "Chemicals", False),
            ("notebook", "Lab Notebook", False),
            ("question", "Quizzes", False),
            ("chart", "Reports && Grades", True),
            ("shield", "Safety Data", False),
            ("cap", "Teacher Dashboard", False),
            ("gear", "Settings", False)
        }

        Dim y As Integer = 90
        For Each item In navItems
            CreateNavItem(item.Item1, item.Item2.Replace("&&", "&"), item.Item3, y)
            y += 46
        Next

        Dim footer As New Panel()
        footer.Size = New Size(sidebar.Width, 64)
        footer.Location = New Point(0, sidebar.Height - 64)
        footer.Anchor = AnchorStyles.Left Or AnchorStyles.Bottom Or AnchorStyles.Right
        footer.BackColor = Color.FromArgb(14, 17, 34)
        sidebar.Controls.Add(footer)

        Dim avatar As New Panel()
        avatar.Size = New Size(38, 38)
        avatar.Location = New Point(20, 13)
        Dim initials As String = GetInitials(userName)
        AddHandler avatar.Paint, Sub(s, e) PaintAvatar(e.Graphics, avatar.Width, avatar.Height, initials)
        footer.Controls.Add(avatar)

        Dim lblName As New Label() With {.Text = userName, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(68, 12)}
        Dim lblRole As New Label() With {.Text = userRole, .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(140, 148, 170), .AutoSize = True, .Location = New Point(68, 32)}
        footer.Controls.Add(lblName)
        footer.Controls.Add(lblRole)

        Dim btnBack As New Label() With {.Text = "⏻", .Font = New Font("Segoe UI", 12), .ForeColor = Color.FromArgb(150, 158, 185),
                                          .Size = New Size(30, 30), .TextAlign = ContentAlignment.MiddleCenter,
                                          .Location = New Point(sidebar.Width - 46, 17), .Cursor = Cursors.Hand}
        AddHandler btnBack.Click, Sub() Me.Close() ' closes this screen, returns to whichever screen opened it
        footer.Controls.Add(btnBack)
    End Sub

    Private Sub NavigateToForm(nextForm As Form)
        AddHandler nextForm.FormClosed, Sub()
                                           Me.Show()
                                           Me.Activate()
                                       End Sub
        Me.Hide()
        nextForm.StartPosition = FormStartPosition.CenterScreen
        nextForm.Show()
    End Sub

    Private Sub CreateNavItem(iconKey As String, label As String, isActive As Boolean, y As Integer)
        Dim item As New RoundedPanel()
        item.CornerRadius = 10
        item.Size = New Size(sidebar.Width - 32, 40)
        item.Location = New Point(16, y)
        item.Cursor = Cursors.Hand
        If isActive Then
            item.FillColor = Color.FromArgb(108, 92, 231)
            item.BorderColor = Color.FromArgb(108, 92, 231)
        Else
            item.FillColor = sidebar.BackColor
            item.BorderColor = sidebar.BackColor
        End If

        Dim iconPanel As New Panel()
        iconPanel.Size = New Size(18, 18)
        iconPanel.Location = New Point(14, 11)
        AddHandler iconPanel.Paint, Sub(s, e) DrawNavIcon(e.Graphics, iconKey, Color.White)
        item.Controls.Add(iconPanel)

        Dim lbl As New Label()
        lbl.Text = label
        lbl.Font = New Font("Segoe UI", 10, If(isActive, FontStyle.Bold, FontStyle.Regular))
        lbl.ForeColor = If(isActive, Color.White, Color.FromArgb(180, 188, 208))
        lbl.AutoSize = True
        lbl.Location = New Point(42, 10)
        item.Controls.Add(lbl)

        If Not isActive Then
            Dim handler As EventHandler = Sub()
                                               Select Case iconKey
                                                   Case "home"
                                                       ' This screen was opened (directly or indirectly) from Home, so
                                                       ' closing it returns to whichever screen is underneath.
                                                       Me.Close()
                                                   Case "grid"
                                                       Try
                                                           NavigateToForm(New ApparatusForm(userName, userRole))
                                                       Catch ex As Exception
                                                           MessageBox.Show($"Failed to open Apparatus: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                       End Try
                                                   Case "beaker"
                                                       Try
                                                           NavigateToForm(New ChemicalsForm(userName, userRole))
                                                       Catch ex As Exception
                                                           MessageBox.Show($"Failed to open Chemicals: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                       End Try
                                                   Case "cap"
                                                       Try
                                                           NavigateToForm(New TeacherDashboardForm(userName, userRole))
                                                       Catch ex As Exception
                                                           MessageBox.Show($"Failed to open Teacher Dashboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                       End Try
                                                   Case "question"
                                                       Try
                                                           NavigateToForm(New Quizzes(userName, userRole))
                                                       Catch ex As Exception
                                                           MessageBox.Show($"Failed to open Quizzes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                       End Try
                                                   Case Else
                                                       MessageBox.Show($"'{label}' is coming soon in a future update.", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                               End Select
                                           End Sub
            AddHandler item.Click, handler
            AddHandler lbl.Click, handler
            AddHandler item.MouseEnter, Sub()
                                             item.FillColor = Color.FromArgb(22, 26, 46)
                                             item.Invalidate()
                                         End Sub
            AddHandler item.MouseLeave, Sub()
                                             item.FillColor = sidebar.BackColor
                                             item.Invalidate()
                                         End Sub
        End If

        sidebar.Controls.Add(item)
    End Sub

    Private Function GetInitials(fullName As String) As String
        ' Guard against empty/blank names and stray double spaces, which would otherwise
        ' throw when taking Substring(0, 1) of an empty split segment.
        If String.IsNullOrWhiteSpace(fullName) Then Return "?"
        Dim parts = fullName.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        If parts.Length >= 2 Then Return (parts(0).Substring(0, 1) & parts(1).Substring(0, 1)).ToUpper()
        If parts.Length = 1 AndAlso parts(0).Length >= 2 Then Return parts(0).Substring(0, 2).ToUpper()
        If parts.Length = 1 Then Return parts(0).ToUpper()
        Return "?"
    End Function

    ' ===================== MAIN CONTENT =====================

    Private Sub BuildContent()
        If content Is Nothing Then
            content = New Panel()
            content.Dock = DockStyle.Fill
            content.BackColor = Color.FromArgb(9, 12, 24)
            content.AutoScroll = True
            Me.Controls.Add(content)
            Me.Controls.SetChildIndex(content, 0)
        Else
            ' Re-entering BuildContent (e.g. on resize/maximize): wipe the previous
            ' controls and rebuild them against the new content size so the layout
            ' reflows instead of staying pinned to the window's original dimensions.
            While content.Controls.Count > 0
                content.Controls(0).Dispose()
            End While
        End If

        Dim lblTitle As New Label() With {.Text = "Reports & Grades", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        content.Controls.Add(lblTitle)

        Dim lblSub As New Label() With {.Text = "Assessment history, topic mastery and downloadable transcripts.",
                                         .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblSub)

        ' ----- top-right actions: Term filter + Export PDF -----
        Dim btnExport As New GradientButton() With {.Text = "⬇  Export PDF", .Size = New Size(140, 36)}
        btnExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnExport.Location = New Point(content.Width - 36 - btnExport.Width, 30)
        AddHandler btnExport.Click, Sub() MessageBox.Show("Exporting transcript as PDF (demo).", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
        content.Controls.Add(btnExport)

        Dim btnTerm As New DarkButton() With {.Text = "⏷  Term 2", .Size = New Size(100, 36)}
        btnTerm.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnTerm.Location = New Point(btnExport.Left - 12 - btnTerm.Width, 30)
        AddHandler btnTerm.Click, Sub() MessageBox.Show("Term filter is coming soon in a future update.", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
        content.Controls.Add(btnTerm)

        ' ----- stat cards row -----
        Dim statGap As Integer = 20
        Dim statY As Integer = 104
        Dim statHeight As Integer = 88
        Dim statWidth As Integer = (content.Width - 72 - statGap * 3) \ 4
        For i As Integer = 0 To stats.Count - 1
            BuildStatCard(stats(i), 36 + i * (statWidth + statGap), statY, statWidth, statHeight)
        Next

        ' ----- charts row -----
        Dim chartY As Integer = statY + statHeight + 20
        Dim chartHeight As Integer = 230
        Dim chartGap As Integer = 20
        Dim trendWidth As Integer = CInt((content.Width - 72 - chartGap) * 0.58)
        Dim masteryWidth As Integer = content.Width - 72 - chartGap - trendWidth

        BuildScoreTrendPanel(36, chartY, trendWidth, chartHeight)
        BuildMasteryPanel(36 + trendWidth + chartGap, chartY, masteryWidth, chartHeight)

        ' ----- assessment history table -----
        Dim tableY As Integer = chartY + chartHeight + 20
        BuildAssessmentHistoryPanel(36, tableY, content.Width - 72)

        BuildBottomToolbar()

        AddHandler content.Resize, Sub()
                                        btnExport.Location = New Point(content.Width - 36 - btnExport.Width, 30)
                                        btnTerm.Location = New Point(btnExport.Left - 12 - btnTerm.Width, 30)
                                    End Sub
    End Sub

    ' ----- stat card -----

    Private Sub BuildStatCard(stat As StatCard, x As Integer, y As Integer, w As Integer, h As Integer)
        Dim card As New RoundedPanel()
        card.CornerRadius = 14
        card.FillColor = Color.FromArgb(16, 20, 40)
        card.BorderColor = Color.FromArgb(36, 41, 66)
        card.Location = New Point(x, y)
        card.Size = New Size(w, h)
        content.Controls.Add(card)

        Dim lblLabel As New Label() With {.Text = stat.Label, .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(140, 148, 170),
                                           .AutoSize = True, .Location = New Point(18, 16), .BackColor = Color.Transparent}
        card.Controls.Add(lblLabel)

        Dim lblValue As New Label() With {.Text = stat.Value, .Font = New Font("Segoe UI", 17, FontStyle.Bold), .ForeColor = Color.White,
                                           .AutoSize = True, .Location = New Point(18, 36), .BackColor = Color.Transparent}
        card.Controls.Add(lblValue)

        Dim lblNote As New Label() With {.Text = stat.Note, .Font = New Font("Segoe UI", 8.5, FontStyle.Bold), .ForeColor = stat.NoteColor,
                                          .AutoSize = True, .Location = New Point(18, 64), .BackColor = Color.Transparent}
        card.Controls.Add(lblNote)
    End Sub

    ' ----- Score trend (line chart) -----

    Private Sub BuildScoreTrendPanel(x As Integer, y As Integer, w As Integer, h As Integer)
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        content.Controls.Add(panel)

        Dim lblTitle As New Label() With {.Text = "📈  Score trend", .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                                           .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, 18), .BackColor = Color.Transparent}
        panel.Controls.Add(lblTitle)

        Dim chartArea As New Panel()
        chartArea.Location = New Point(16, 50)
        chartArea.Size = New Size(w - 32, h - 66)
        chartArea.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        chartArea.BackColor = Color.Transparent
        AddHandler chartArea.Paint, AddressOf PaintScoreTrend
        panel.Controls.Add(chartArea)
    End Sub

    Private Sub PaintScoreTrend(sender As Object, e As PaintEventArgs)
        Dim area As Panel = DirectCast(sender, Panel)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim leftPad As Integer = 34
        Dim bottomPad As Integer = 20
        Dim topPad As Integer = 6
        Dim plotW As Integer = area.Width - leftPad - 8
        Dim plotH As Integer = area.Height - bottomPad - topPad
        If plotW < 20 OrElse plotH < 20 Then Return

        Dim gridValues As Integer() = {100, 85, 70, 55, 40}
        Dim minVal As Integer = 40
        Dim maxVal As Integer = 100

        Using gridPen As New Pen(Color.FromArgb(26, 30, 50), 1)
            Using axisFont As New Font("Segoe UI", 7.5)
                Using axisBrush As New SolidBrush(Color.FromArgb(110, 118, 140))
                    For Each gv In gridValues
                        Dim ratio As Double = (gv - minVal) / CDbl(maxVal - minVal)
                        Dim gy As Single = topPad + plotH - CSng(ratio * plotH)
                        g.DrawLine(gridPen, leftPad, gy, leftPad + plotW, gy)
                        Dim sz = g.MeasureString(gv.ToString(), axisFont)
                        g.DrawString(gv.ToString(), axisFont, axisBrush, leftPad - sz.Width - 6, gy - sz.Height / 2)
                    Next
                End Using
            End Using
        End Using

        If scoreTrend.Count = 0 Then Return

        Dim points As New List(Of PointF)
        Dim stepX As Single = If(scoreTrend.Count > 1, plotW / CSng(scoreTrend.Count - 1), 0)
        For i As Integer = 0 To scoreTrend.Count - 1
            Dim ratio As Double = (scoreTrend(i).Score - minVal) / CDbl(maxVal - minVal)
            Dim px As Single = leftPad + i * stepX
            Dim py As Single = topPad + plotH - CSng(ratio * plotH)
            points.Add(New PointF(px, py))
        Next

        ' filled area under the line
        If points.Count > 1 Then
            Dim fillPts As New List(Of PointF)(points)
            fillPts.Add(New PointF(points(points.Count - 1).X, topPad + plotH))
            fillPts.Add(New PointF(points(0).X, topPad + plotH))
            Using fillBrush As New LinearGradientBrush(New Rectangle(leftPad, topPad, plotW, plotH),
                                                        Color.FromArgb(60, 60, 200, 190), Color.FromArgb(0, 60, 200, 190), 90.0F)
                g.FillPolygon(fillBrush, fillPts.ToArray())
            End Using

            Using linePen As New Pen(Color.FromArgb(60, 200, 190), 2.4F)
                linePen.LineJoin = LineJoin.Round
                g.DrawLines(linePen, points.ToArray())
            End Using
        End If

        Using markerBrush As New SolidBrush(Color.FromArgb(60, 200, 190))
            For Each p In points
                g.FillEllipse(markerBrush, p.X - 3.5F, p.Y - 3.5F, 7, 7)
                g.FillEllipse(Brushes.White, p.X - 1.5F, p.Y - 1.5F, 3, 3)
            Next
        End Using

        Using labelFont As New Font("Segoe UI", 7.5)
            Using labelBrush As New SolidBrush(Color.FromArgb(110, 118, 140))
                For i As Integer = 0 To scoreTrend.Count - 1
                    Dim sz = g.MeasureString(scoreTrend(i).Month, labelFont)
                    g.DrawString(scoreTrend(i).Month, labelFont, labelBrush, points(i).X - sz.Width / 2, topPad + plotH + 4)
                Next
            End Using
        End Using
    End Sub

    ' ----- Mastery by topic (bar chart) -----

    Private Sub BuildMasteryPanel(x As Integer, y As Integer, w As Integer, h As Integer)
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        content.Controls.Add(panel)

        Dim lblTitle As New Label() With {.Text = "🧪  Mastery by topic", .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                                           .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, 18), .BackColor = Color.Transparent}
        panel.Controls.Add(lblTitle)

        Dim chartArea As New Panel()
        chartArea.Location = New Point(16, 50)
        chartArea.Size = New Size(w - 32, h - 66)
        chartArea.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        chartArea.BackColor = Color.Transparent
        AddHandler chartArea.Paint, AddressOf PaintMasteryChart
        panel.Controls.Add(chartArea)
    End Sub

    Private Sub PaintMasteryChart(sender As Object, e As PaintEventArgs)
        Dim area As Panel = DirectCast(sender, Panel)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim leftPad As Integer = 30
        Dim bottomPad As Integer = 20
        Dim topPad As Integer = 6
        Dim plotW As Integer = area.Width - leftPad - 8
        Dim plotH As Integer = area.Height - bottomPad - topPad
        If plotW < 20 OrElse plotH < 20 OrElse masteryTopics.Count = 0 Then Return

        Dim gridValues As Integer() = {100, 75, 50, 25, 0}
        Using gridPen As New Pen(Color.FromArgb(26, 30, 50), 1)
            Using axisFont As New Font("Segoe UI", 7.5)
                Using axisBrush As New SolidBrush(Color.FromArgb(110, 118, 140))
                    For Each gv In gridValues
                        Dim ratio As Double = gv / 100.0
                        Dim gy As Single = topPad + plotH - CSng(ratio * plotH)
                        g.DrawLine(gridPen, leftPad, gy, leftPad + plotW, gy)
                        Dim sz = g.MeasureString(gv.ToString(), axisFont)
                        g.DrawString(gv.ToString(), axisFont, axisBrush, leftPad - sz.Width - 6, gy - sz.Height / 2)
                    Next
                End Using
            End Using
        End Using

        Dim n As Integer = masteryTopics.Count
        Dim slot As Single = plotW / CSng(n)
        Dim barWidth As Single = Math.Min(34, slot * 0.5F)

        Using labelFont As New Font("Segoe UI", 7.5)
            Using labelBrush As New SolidBrush(Color.FromArgb(150, 158, 180))
                For i As Integer = 0 To n - 1
                    Dim pct As Integer = masteryTopics(i).Percent
                    Dim barH As Single = CSng(plotH * (pct / 100.0))
                    Dim bx As Single = leftPad + i * slot + (slot - barWidth) / 2
                    Dim by As Single = topPad + plotH - barH

                    Dim barRect As New RectangleF(bx, by, barWidth, barH)
                    Using path = RoundedRectPath(Rectangle.Round(barRect), 6)
                        Using br As New LinearGradientBrush(New RectangleF(bx, by, barWidth, Math.Max(barH, 1)),
                                                             Color.FromArgb(108, 92, 231), Color.FromArgb(150, 130, 240), 90.0F)
                            g.FillPath(br, path)
                        End Using
                    End Using

                    Dim topicLabel As String = masteryTopics(i).Topic
                    Dim sz = g.MeasureString(topicLabel, labelFont)
                    g.DrawString(topicLabel, labelFont, labelBrush, leftPad + i * slot + (slot - sz.Width) / 2, topPad + plotH + 4)
                Next
            End Using
        End Using
    End Sub

    ' ----- Assessment history table -----

    Private Sub BuildAssessmentHistoryPanel(x As Integer, y As Integer, w As Integer)
        Dim rowHeight As Integer = 44
        Dim headerHeight As Integer = 50
        Dim h As Integer = headerHeight + 36 + assessments.Count * rowHeight + 16

        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        panel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        content.Controls.Add(panel)

        Dim lblTitle As New Label() With {.Text = "📄  Assessment history", .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                                           .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, 18), .BackColor = Color.Transparent}
        panel.Controls.Add(lblTitle)

        ' column x-offsets (proportion of inner width, inner width = w - 40)
        Dim colName As Integer = 20
        Dim colType As Integer = CInt(w * 0.34)
        Dim colDate As Integer = CInt(w * 0.50)
        Dim colScore As Integer = CInt(w * 0.66)
        Dim colStatus As Integer = CInt(w * 0.76)
        Dim colAction As Integer = w - 120

        Dim headerY As Integer = 58
        Dim headers As (String, Integer)() = {
            ("Assessment", colName), ("Type", colType), ("Date", colDate), ("Score", colScore), ("Status", colStatus)
        }
        For Each hd In headers
            Dim lblH As New Label() With {.Text = hd.Item1, .Font = New Font("Segoe UI", 8.5, FontStyle.Bold), .ForeColor = Color.FromArgb(120, 128, 152),
                                           .AutoSize = True, .Location = New Point(hd.Item2, headerY), .BackColor = Color.Transparent}
            panel.Controls.Add(lblH)
        Next

        Dim divider As New Panel() With {.Height = 1, .BackColor = Color.FromArgb(30, 34, 56), .Location = New Point(20, headerY + 26)}
        divider.Width = w - 40
        divider.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        panel.Controls.Add(divider)

        Dim rowY As Integer = headerY + 36
        For Each a In assessments
            Dim lblName As New Label() With {.Text = a.Name, .Font = New Font("Segoe UI", 9.5, FontStyle.Bold), .ForeColor = Color.White,
                                              .AutoSize = True, .Location = New Point(colName, rowY + 12), .BackColor = Color.Transparent}
            Dim lblType As New Label() With {.Text = a.Type, .Font = New Font("Segoe UI", 9), .ForeColor = Color.FromArgb(150, 170, 255),
                                              .AutoSize = True, .Location = New Point(colType, rowY + 12), .BackColor = Color.Transparent}
            Dim lblDate As New Label() With {.Text = a.DateText, .Font = New Font("Segoe UI", 9), .ForeColor = Color.FromArgb(170, 176, 196),
                                              .AutoSize = True, .Location = New Point(colDate, rowY + 12), .BackColor = Color.Transparent}
            Dim lblScore As New Label() With {.Text = a.Score, .Font = New Font("Segoe UI", 9.5, FontStyle.Bold), .ForeColor = Color.White,
                                               .AutoSize = True, .Location = New Point(colScore, rowY + 12), .BackColor = Color.Transparent}
            panel.Controls.Add(lblName)
            panel.Controls.Add(lblType)
            panel.Controls.Add(lblDate)
            panel.Controls.Add(lblScore)

            Dim isGraded As Boolean = a.Status.Equals("Graded", StringComparison.OrdinalIgnoreCase)
            Dim pill As New RoundedPanel()
            pill.CornerRadius = 10
            pill.FillColor = If(isGraded, Color.FromArgb(18, 42, 34), Color.FromArgb(46, 34, 18))
            pill.BorderColor = pill.FillColor
            pill.Size = New Size(64, 22)
            pill.Location = New Point(colStatus, rowY + 11)
            panel.Controls.Add(pill)

            Dim lblStatus As New Label() With {.Text = a.Status, .Font = New Font("Segoe UI", 8, FontStyle.Bold),
                                                .ForeColor = If(isGraded, Color.FromArgb(120, 220, 170), Color.FromArgb(230, 170, 100)),
                                                .AutoSize = False, .Size = New Size(64, 22), .TextAlign = ContentAlignment.MiddleCenter,
                                                .BackColor = Color.Transparent}
            pill.Controls.Add(lblStatus)

            Dim lblAction As New Label() With {.Text = "View report", .Font = New Font("Segoe UI", 8.5, FontStyle.Bold),
                                                .ForeColor = Color.FromArgb(150, 170, 255), .AutoSize = True,
                                                .Location = New Point(colAction, rowY + 12), .Cursor = Cursors.Hand, .BackColor = Color.Transparent}
            AddHandler lblAction.Click, Sub() MessageBox.Show($"Opening report for '{a.Name}' (demo).", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
            panel.Controls.Add(lblAction)

            If rowY + rowHeight < h - 10 Then
                Dim rowDivider As New Panel() With {.Height = 1, .BackColor = Color.FromArgb(24, 28, 46), .Location = New Point(20, rowY + rowHeight - 4)}
                rowDivider.Width = w - 40
                rowDivider.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
                panel.Controls.Add(rowDivider)
            End If

            rowY += rowHeight
        Next
    End Sub

    ' ----- floating bottom toolbar -----

    Private Sub BuildBottomToolbar()
        Dim toolbar As New RoundedPanel()
        toolbar.CornerRadius = 20
        toolbar.FillColor = Color.FromArgb(18, 22, 40)
        toolbar.BorderColor = Color.FromArgb(40, 45, 70)
        toolbar.Size = New Size(180, 40)
        toolbar.Anchor = AnchorStyles.Bottom
        content.Controls.Add(toolbar)
        toolbar.BringToFront()

        Dim icons As String() = {"⟲", "T", "✎", "💬"}
        Dim ix As Integer = 16
        For Each ic In icons
            Dim lbl As New Label() With {.Text = ic, .Font = New Font("Segoe UI", 10), .ForeColor = Color.FromArgb(180, 188, 208),
                                          .AutoSize = True, .Location = New Point(ix, 10), .Cursor = Cursors.Hand}
            toolbar.Controls.Add(lbl)
            ix += 38
        Next

        AddHandler content.Resize, Sub()
                                        toolbar.Location = New Point((content.Width - toolbar.Width) \ 2, content.Height - 60)
                                    End Sub
        toolbar.Location = New Point((content.Width - toolbar.Width) \ 2, content.Height - 60)
    End Sub

    ' ===================== SHARED DRAWING =====================

    Private Sub PaintLogoIcon(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, 43, 43)
        Using path = RoundedRectPath(rect, 12)
            Using br As New LinearGradientBrush(rect, Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205), 45.0F)
                g.FillPath(br, path)
            End Using
        End Using
        Dim cx As Single = rect.Width / 2.0F
        Dim cy As Single = rect.Height / 2.0F
        Using flaskPen As New Pen(Color.White, 2.0F)
            flaskPen.LineJoin = LineJoin.Round
            flaskPen.StartCap = LineCap.Round
            flaskPen.EndCap = LineCap.Round
            Dim neckTopL As New PointF(cx - 3, cy - 12)
            Dim neckTopR As New PointF(cx + 3, cy - 12)
            Dim neckBotL As New PointF(cx - 3, cy - 1)
            Dim neckBotR As New PointF(cx + 3, cy - 1)
            Dim bodyL As New PointF(cx - 11, cy + 11)
            Dim bodyR As New PointF(cx + 11, cy + 11)
            g.DrawLine(flaskPen, New PointF(neckTopL.X - 2, neckTopL.Y), New PointF(neckTopR.X + 2, neckTopR.Y))
            g.DrawLine(flaskPen, neckTopL, neckBotL)
            g.DrawLine(flaskPen, neckTopR, neckBotR)
            g.DrawLine(flaskPen, neckBotL, bodyL)
            g.DrawLine(flaskPen, neckBotR, bodyR)
            Dim basePts() As PointF = {bodyL, New PointF(bodyL.X + 3, bodyR.Y + 3), New PointF(bodyR.X - 3, bodyR.Y + 3), bodyR}
            g.DrawLines(flaskPen, basePts)
        End Using
    End Sub

    Private Sub PaintAvatar(g As Graphics, w As Integer, h As Integer, initials As String)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, w - 1, h - 1)
        Using br As New LinearGradientBrush(rect, Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205), 45.0F)
            g.FillEllipse(br, rect)
        End Using
        Using f As New Font("Segoe UI", 9, FontStyle.Bold)
            Dim sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            g.DrawString(initials, f, Brushes.White, rect, sf)
        End Using
    End Sub

    Private Sub DrawNavIcon(g As Graphics, key As String, color As Color)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using pen As New Pen(color, 1.6F)
            Select Case key
                Case "home"
                    g.DrawLines(pen, {New PointF(1, 9), New PointF(9, 1), New PointF(17, 9)})
                    g.DrawRectangle(pen, 3, 9, 12, 8)
                Case "flask", "beaker"
                    g.DrawLine(pen, 6, 1, 6, 7) : g.DrawLine(pen, 12, 1, 12, 7)
                    g.DrawLine(pen, 6, 7, 2, 17) : g.DrawLine(pen, 12, 7, 16, 17)
                    g.DrawLine(pen, 2, 17, 16, 17)
                Case "book", "notebook"
                    g.DrawRectangle(pen, 2, 2, 14, 14)
                    g.DrawLine(pen, 2, 7, 16, 7)
                Case "grid"
                    g.DrawRectangle(pen, 2, 2, 6, 6) : g.DrawRectangle(pen, 10, 2, 6, 6)
                    g.DrawRectangle(pen, 2, 10, 6, 6) : g.DrawRectangle(pen, 10, 10, 6, 6)
                Case "question"
                    g.DrawEllipse(pen, 1, 1, 16, 16)
                    Using f As New Font("Segoe UI", 8, FontStyle.Bold)
                        g.DrawString("?", f, New SolidBrush(color), 5, 2)
                    End Using
                Case "chart"
                    g.DrawLine(pen, 2, 16, 2, 8) : g.DrawLine(pen, 8, 16, 8, 3) : g.DrawLine(pen, 14, 16, 14, 10)
                Case "shield"
                    g.DrawLines(pen, {New PointF(9, 1), New PointF(17, 4), New PointF(17, 9), New PointF(9, 17), New PointF(1, 9), New PointF(1, 4), New PointF(9, 1)})
                Case "cap"
                    g.DrawLines(pen, {New PointF(1, 6), New PointF(9, 1), New PointF(17, 6), New PointF(9, 11), New PointF(1, 6)})
                    g.DrawLine(pen, 4, 8, 4, 13)
                Case "gear"
                    g.DrawEllipse(pen, 4, 4, 10, 10)
                    g.DrawEllipse(pen, 7, 7, 4, 4)
                Case Else
                    g.DrawEllipse(pen, 2, 2, 14, 14)
            End Select
        End Using
    End Sub

End Class