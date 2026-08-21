Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows.Forms

''' <summary>
''' "Teacher Dashboard" screen: live class overview, student progress table,
''' average-by-class chart and a grading queue. Mirrors the sidebar/title-bar
''' chrome used by the other screens (HomeForm, ApparatusForm, ChemicalsForm)
''' so every screen feels like the same app.
''' </summary>
Public Class TeacherDashboardForm
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
    Private titleBar As Panel
    Private content As Panel

    ' Student, Class, Completed ("14/20"), Average ("86%"), Status ("In lab" / "Offline")
    ' Offline fallback shown immediately; LoadFromDbAsync() (fired from Form.Load)
    ' replaces this with real roster data (and re-renders) once it arrives.
    Private students As (String, String, String, String, String)() = {
        ("Mac Falen", "Grade 11-B", "14/20", "86%", "In lab"),
        ("Aisha Bello", "Grade 11-B", "17/20", "91%", "In lab"),
        ("Tom Meier", "Grade 11-A", "9/20", "68%", "Offline"),
        ("Lina Ortiz", "Grade 11-A", "12/20", "79%", "Offline"),
        ("Kwame Adjei", "Grade 12-C", "20/20", "94%", "In lab")
    }

    ' Class label, average score out of 100. Same offline-fallback pattern.
    Private classAverages As (String, Integer)() = {
        ("11-A", 72), ("11-B", 78), ("12-C", 85), ("12-D", 55)
    }

    ' Assessment id (0 for offline fallback rows — can't be graded for real),
    ' report/quiz title, student name. Same offline-fallback pattern.
    Private gradingQueue As (Integer, String, String)() = {
        (0, "Titration report", "Aisha Bello"),
        (0, "Gas Evolution report", "Tom Meier"),
        (0, "Flame Test quiz", "Lina Ortiz")
    }

    ' Offline fallback for the four stat cards.
    Private teacherStats As (StudentsEnrolled As Integer, LiveInLabNow As Integer, AwaitingGrading As Integer, ClassAverage As Decimal) =
        (128, 23, 11, 81D)

    Public Sub New(Optional displayName As String = "Mac Falen", Optional role As String = "Teacher")
        userName = If(String.IsNullOrWhiteSpace(displayName), "Teacher", displayName)
        userRole = If(String.IsNullOrWhiteSpace(role), "Teacher", role)

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1200, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.AutoScroll = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Teacher Dashboard"

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

        AddHandler Me.Load, Sub(s, e) LoadFromDbAsync(s, e)
    End Sub

    ''' <summary>
    ''' Loads real stats, roster, class averages, and grading queue from the
    ''' database, replacing the offline fallback and re-rendering. Silently
    ''' keeps the fallback if the database isn't reachable.
    ''' </summary>
    Private Async Function LoadFromDbAsync(sender As Object, e As EventArgs) As Task
        Try
            Dim statsTask = TeacherRepository.GetStatsAsync()
            Dim studentsTask = TeacherRepository.GetStudentsOverviewAsync()
            Dim classAvgTask = TeacherRepository.GetClassAveragesAsync()
            Dim queueTask = TeacherRepository.GetGradingQueueAsync()
            Await Task.WhenAll(statsTask, studentsTask, classAvgTask, queueTask)

            teacherStats = (statsTask.Result.StudentsEnrolled, statsTask.Result.LiveInLabNow,
                             statsTask.Result.AwaitingGrading, statsTask.Result.ClassAverage)

            If studentsTask.Result.Count > 0 Then
                students = studentsTask.Result.Select(Function(s) (s.Name, s.ClassName, s.Completed, s.Average, s.Status)).ToArray()
            End If
            If classAvgTask.Result.Count > 0 Then
                classAverages = classAvgTask.Result.Select(Function(c) (c.ClassName, c.Average)).ToArray()
            End If
            ' Grading queue legitimately can be empty (nothing pending), so
            ' replace unconditionally rather than only when non-empty.
            gradingQueue = queueTask.Result.Select(Function(q) (q.AssessmentId, q.Title, q.StudentName)).ToArray()

            BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not load teacher dashboard data from database: {ex.Message}")
        End Try
    End Function

    ' ===================== TITLE BAR =====================

    Private Sub BuildTitleBar()
        titleBar = New Panel()
        titleBar.Dock = DockStyle.Top
        titleBar.Height = 40
        titleBar.BackColor = Color.FromArgb(9, 12, 24)
        Me.Controls.Add(titleBar)

        Dim lblTitle As New Label()
        lblTitle.Text = "ChemLab Virtual — Teacher Dashboard"
        lblTitle.Font = New Font("Segoe UI", 9.5, FontStyle.Regular)
        lblTitle.ForeColor = Color.FromArgb(150, 158, 185)
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(16, 11)
        titleBar.Controls.Add(lblTitle)

        AddHandler titleBar.MouseDown, Sub(s, e)
                                            If e.Button = MouseButtons.Left Then
                                                ReleaseCapture()
                                                SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
                                            End If
                                        End Sub

        Dim btnClose As New Label()
        btnClose.Text = "✕"
        btnClose.Font = New Font("Segoe UI", 10)
        btnClose.ForeColor = Color.FromArgb(160, 168, 190)
        btnClose.Size = New Size(40, 40)
        btnClose.TextAlign = ContentAlignment.MiddleCenter
        btnClose.Cursor = Cursors.Hand
        btnClose.Dock = DockStyle.Right
        AddHandler btnClose.Click, Sub() Me.Close()
        AddHandler btnClose.MouseEnter, Sub() btnClose.BackColor = Color.FromArgb(200, 60, 60)
        AddHandler btnClose.MouseLeave, Sub() btnClose.BackColor = Color.Transparent

        Dim btnMax As New Label()
        btnMax.Text = "☐"
        btnMax.Font = New Font("Segoe UI", 9)
        btnMax.ForeColor = Color.FromArgb(160, 168, 190)
        btnMax.Size = New Size(40, 40)
        btnMax.TextAlign = ContentAlignment.MiddleCenter
        btnMax.Cursor = Cursors.Hand
        btnMax.Dock = DockStyle.Right
        AddHandler btnMax.Click, Sub()
                                      Me.WindowState = If(Me.WindowState = FormWindowState.Maximized, FormWindowState.Normal, FormWindowState.Maximized)
                                  End Sub
        AddHandler btnMax.MouseEnter, Sub() btnMax.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMax.MouseLeave, Sub() btnMax.BackColor = Color.Transparent

        Dim btnMin As New Label()
        btnMin.Text = "—"
        btnMin.Font = New Font("Segoe UI", 9)
        btnMin.ForeColor = Color.FromArgb(160, 168, 190)
        btnMin.Size = New Size(40, 40)
        btnMin.TextAlign = ContentAlignment.MiddleCenter
        btnMin.Cursor = Cursors.Hand
        btnMin.Dock = DockStyle.Right
        AddHandler btnMin.Click, Sub() Me.WindowState = FormWindowState.Minimized
        AddHandler btnMin.MouseEnter, Sub() btnMin.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMin.MouseLeave, Sub() btnMin.BackColor = Color.Transparent

        titleBar.Controls.Add(btnClose)
        titleBar.Controls.Add(btnMax)
        titleBar.Controls.Add(btnMin)
    End Sub

    ' ===================== SIDEBAR =====================

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

        Dim lblChem As New Label()
        lblChem.Text = "ChemLab"
        lblChem.Font = New Font("Segoe UI", 13, FontStyle.Bold)
        lblChem.ForeColor = Color.White
        lblChem.AutoSize = True
        lblChem.Location = New Point(74, 20)
        sidebar.Controls.Add(lblChem)

        Dim lblVirtual As New Label()
        lblVirtual.Text = "V I R T U A L"
        lblVirtual.Font = New Font("Segoe UI", 7.5)
        lblVirtual.ForeColor = Color.FromArgb(150, 158, 185)
        lblVirtual.AutoSize = True
        lblVirtual.Location = New Point(75, 43)
        sidebar.Controls.Add(lblVirtual)

        Dim navItems As (String, String, Boolean)() = {
            ("home", "Home", False),
            ("flask", "Lab Workspace", False),
            ("book", "Experiments", False),
            ("grid", "Apparatus", False),
            ("beaker", "Chemicals", False),
            ("notebook", "Lab Notebook", False),
            ("question", "Quizzes", False),
            ("chart", "Reports && Grades", False),
            ("shield", "Safety Data", False),
            ("cap", "Teacher Dashboard", True),
            ("gear", "Settings", False)
        }

        Dim y As Integer = 90
        For Each item In navItems
            CreateNavItem(item.Item1, item.Item2.Replace("&&", "&"), item.Item3, y)
            y += 46
        Next

        ' profile footer
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

        Dim lblName As New Label()
        lblName.Text = userName
        lblName.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        lblName.ForeColor = Color.White
        lblName.AutoSize = True
        lblName.Location = New Point(68, 12)
        footer.Controls.Add(lblName)

        Dim lblRole As New Label()
        lblRole.Text = userRole
        lblRole.Font = New Font("Segoe UI", 8.5)
        lblRole.ForeColor = Color.FromArgb(140, 148, 170)
        lblRole.AutoSize = True
        lblRole.Location = New Point(68, 32)
        footer.Controls.Add(lblRole)

        Dim btnBack As New Label()
        btnBack.Text = "⏻"
        btnBack.Font = New Font("Segoe UI", 12)
        btnBack.ForeColor = Color.FromArgb(150, 158, 185)
        btnBack.Size = New Size(30, 30)
        btnBack.TextAlign = ContentAlignment.MiddleCenter
        btnBack.Location = New Point(sidebar.Width - 46, 17)
        btnBack.Cursor = Cursors.Hand
        AddHandler btnBack.Click, Sub()
                                       Dim confirm = MessageBox.Show("Log out of ChemLab Virtual?", "Log out", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                                       If confirm = DialogResult.Yes Then
                                           Me.DialogResult = DialogResult.Retry
                                           Me.Close()
                                       End If
                                   End Sub
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

        If isActive Then
            ' Already on the Teacher Dashboard screen — nothing to do.
        ElseIf iconKey = "home" Then
            ' This screen was opened (directly or indirectly) from Home, so closing it
            ' returns to whichever screen is underneath instead of stacking a new one.
            Dim goHome As EventHandler = Sub() Me.Close()
            AddHandler item.Click, goHome
            AddHandler lbl.Click, goHome
        ElseIf iconKey = "grid" Then
            Dim openApp As EventHandler = Sub()
                                              Try
                                                  NavigateToForm(New ApparatusForm(userName, userRole))
                                              Catch ex As Exception
                                                  MessageBox.Show($"Failed to open Apparatus: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                              End Try
                                          End Sub
            AddHandler item.Click, openApp
            AddHandler lbl.Click, openApp
        ElseIf iconKey = "beaker" Then
            Dim openChem As EventHandler = Sub()
                                               Try
                                                   NavigateToForm(New ChemicalsForm(userName, userRole))
                                               Catch ex As Exception
                                                   MessageBox.Show($"Failed to open Chemicals: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                               End Try
                                           End Sub
            AddHandler item.Click, openChem
            AddHandler lbl.Click, openChem
        ElseIf iconKey = "chart" Then
            Dim openReports As EventHandler = Sub()
                                                 Try
                                                     NavigateToForm(New ReportsGrades(userName, userRole))
                                                 Catch ex As Exception
                                                     MessageBox.Show($"Failed to open Reports & Grades: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                 End Try
                                             End Sub
            AddHandler item.Click, openReports
            AddHandler lbl.Click, openReports
        ElseIf iconKey = "book" Then
            Dim openExperiments As EventHandler = Sub()
                                                       Try
                                                           NavigateToForm(New ExperimentsForm(userName, userRole))
                                                       Catch ex As Exception
                                                           MessageBox.Show($"Failed to open Experiments: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                       End Try
                                                   End Sub
            AddHandler item.Click, openExperiments
            AddHandler lbl.Click, openExperiments
        ElseIf iconKey = "question" Then
            Dim openQuizzes As EventHandler = Sub()
                                                   Try
                                                       NavigateToForm(New Quizzes(userName, userRole))
                                                   Catch ex As Exception
                                                       MessageBox.Show($"Failed to open Quizzes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                   End Try
                                               End Sub
            AddHandler item.Click, openQuizzes
            AddHandler lbl.Click, openQuizzes
        Else
            Dim handler As EventHandler = Sub() MessageBox.Show($"'{label}' is coming soon in a future update.", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
            AddHandler item.Click, handler
            AddHandler lbl.Click, handler
        End If

        If Not isActive Then
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

        Dim lblTitle As New Label()
        lblTitle.Text = "Teacher Dashboard"
        lblTitle.Font = New Font("Segoe UI", 22, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(36, 22)
        content.Controls.Add(lblTitle)

        Dim lblSub As New Label()
        lblSub.Text = "Monitor live lab sessions, assign experiments and review class performance."
        lblSub.Font = New Font("Segoe UI", 10.5)
        lblSub.ForeColor = Color.FromArgb(140, 148, 210)
        lblSub.AutoSize = True
        lblSub.Location = New Point(36, 56)
        content.Controls.Add(lblSub)

        Dim btnAssign As New GradientButton()
        btnAssign.Text = "+  Assign experiment"
        btnAssign.Size = New Size(190, 38)
        btnAssign.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnAssign.Location = New Point(content.Width - 190 - 36, 20)
        AddHandler btnAssign.Click, Sub() MessageBox.Show("Choose an experiment and a class to assign it to.", "ChemLab Virtual")
        content.Controls.Add(btnAssign)

        Dim btnQueue As New DarkButton()
        btnQueue.Text = "🔖  Grade queue"
        btnQueue.Size = New Size(148, 38)
        btnQueue.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnQueue.Location = New Point(btnAssign.Location.X - 148 - 12, 20)
        AddHandler btnQueue.Click, Sub() MessageBox.Show("Opens the full grading queue.", "ChemLab Virtual")
        content.Controls.Add(btnQueue)

        BuildStatCards()
        BuildStudentsPanel()
        BuildAverageByClassPanel()
        BuildGradingQueuePanel()
    End Sub

    ' ---------- Stat cards ----------

    Private Sub BuildStatCards()
        Dim stats As (String, String, String, Color)() = {
            ("person", "Students enrolled", teacherStats.StudentsEnrolled.ToString(), Color.FromArgb(108, 92, 231)),
            ("live", "Live in lab now", teacherStats.LiveInLabNow.ToString(), Color.FromArgb(92, 150, 231)),
            ("clipboard", "Awaiting grading", teacherStats.AwaitingGrading.ToString(), Color.FromArgb(92, 130, 200)),
            ("target", "Class average", $"{Math.Round(teacherStats.ClassAverage, 0)}%", Color.FromArgb(150, 92, 231))
        }
        Dim gap As Integer = 20
        Dim totalW As Integer = content.Width - 72
        Dim cardW As Integer = (totalW - gap * (stats.Length - 1)) \ stats.Length

        For i As Integer = 0 To stats.Length - 1
            CreateStatCard(stats(i).Item1, stats(i).Item2, stats(i).Item3, stats(i).Item4, 36 + i * (cardW + gap), 110, cardW)
        Next
    End Sub

    Private Sub CreateStatCard(iconKey As String, label As String, value As String, tint As Color, x As Integer, y As Integer, w As Integer)
        Dim card As New RoundedPanel()
        card.CornerRadius = 14
        card.FillColor = Color.FromArgb(16, 20, 40)
        card.BorderColor = Color.FromArgb(36, 41, 66)
        card.Location = New Point(x, y)
        card.Size = New Size(w, 78)
        card.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        content.Controls.Add(card)

        Dim iconTile As New RoundedPanel()
        iconTile.CornerRadius = 10
        iconTile.FillColor = Color.FromArgb(tint.R \ 3 + 12, tint.G \ 3 + 14, tint.B \ 3 + 26)
        iconTile.BorderColor = iconTile.FillColor
        iconTile.Size = New Size(38, 38)
        iconTile.Location = New Point(16, 20)
        AddHandler iconTile.Paint, Sub(s, e) DrawStatIcon(e.Graphics, iconKey, tint)
        card.Controls.Add(iconTile)

        Dim lblLabel As New Label()
        lblLabel.Text = label
        lblLabel.Font = New Font("Segoe UI", 8.25)
        lblLabel.ForeColor = Color.FromArgb(140, 148, 170)
        lblLabel.AutoSize = True
        lblLabel.Location = New Point(66, 20)
        card.Controls.Add(lblLabel)

        Dim lblValue As New Label()
        lblValue.Text = value
        lblValue.Font = New Font("Segoe UI", 15, FontStyle.Bold)
        lblValue.ForeColor = Color.White
        lblValue.AutoSize = True
        lblValue.Location = New Point(66, 38)
        card.Controls.Add(lblValue)
    End Sub

    Private Sub DrawStatIcon(g As Graphics, key As String, color As Color)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using pen As New Pen(color, 1.7F)
            Select Case key
                Case "person"
                    g.DrawEllipse(pen, 13, 6, 12, 12)
                    g.DrawArc(pen, 7, 20, 24, 16, 180, 180)
                Case "live"
                    g.DrawRectangle(pen, 6, 11, 18, 16)
                    g.DrawLines(pen, {New PointF(24, 16), New PointF(32, 11), New PointF(32, 27), New PointF(24, 22)})
                Case "clipboard"
                    g.DrawRectangle(pen, 9, 8, 20, 24)
                    g.DrawRectangle(pen, 14, 5, 10, 6)
                    g.DrawLine(pen, 13, 18, 25, 18)
                    g.DrawLine(pen, 13, 24, 25, 24)
                Case "target"
                    g.DrawEllipse(pen, 6, 6, 26, 26)
                    g.DrawEllipse(pen, 13, 13, 12, 12)
                Case Else
                    g.DrawEllipse(pen, 8, 8, 22, 22)
            End Select
        End Using
    End Sub

    ' ---------- Students table ----------

    Private Sub BuildStudentsPanel()
        Dim gap As Integer = 24
        Dim totalW As Integer = content.Width - 72
        Dim leftW As Integer = CInt(totalW * 0.58)
        Dim panelY As Integer = 212
        Dim panelH As Integer = 340

        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(36, panelY)
        panel.Size = New Size(leftW, panelH)
        panel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Bottom
        content.Controls.Add(panel)

        Dim lblTitle As New Label()
        lblTitle.Text = "👥  Students"
        lblTitle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(20, 18)
        panel.Controls.Add(lblTitle)

        Dim btnSearch As New DarkButton()
        btnSearch.Text = "🔍  Search"
        btnSearch.Size = New Size(94, 30)
        btnSearch.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnSearch.Location = New Point(leftW - 94 - 16, 12)
        AddHandler btnSearch.Click, Sub() MessageBox.Show("Filter the student list by name or class.", "ChemLab Virtual")
        panel.Controls.Add(btnSearch)

        ' Column headers
        Dim colX As Integer() = {20, CInt(leftW * 0.30), CInt(leftW * 0.52), CInt(leftW * 0.68), CInt(leftW * 0.82)}
        Dim headers As String() = {"Student", "Class", "Completed", "Average", "Status"}
        Dim headerY As Integer = 58
        For i As Integer = 0 To headers.Length - 1
            Dim lblH As New Label()
            lblH.Text = headers(i)
            lblH.Font = New Font("Segoe UI", 8.25, FontStyle.Bold)
            lblH.ForeColor = Color.FromArgb(120, 130, 220)
            lblH.AutoSize = True
            lblH.Location = New Point(colX(i), headerY)
            panel.Controls.Add(lblH)
        Next

        Dim divider As New Panel()
        divider.Size = New Size(leftW - 40, 1)
        divider.BackColor = Color.FromArgb(36, 41, 66)
        divider.Location = New Point(20, headerY + 22)
        divider.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        panel.Controls.Add(divider)

        Dim rowY As Integer = headerY + 34
        For Each s In students
            Dim isInLab As Boolean = (s.Item5 = "In lab")

            Dim lblName As New Label()
            lblName.Text = s.Item1
            lblName.Font = New Font("Segoe UI", 9.5, FontStyle.Bold)
            lblName.ForeColor = If(isInLab, Color.FromArgb(94, 234, 212), Color.White)
            lblName.AutoSize = True
            lblName.Location = New Point(colX(0), rowY)
            panel.Controls.Add(lblName)

            Dim lblClass As New Label()
            lblClass.Text = s.Item2
            lblClass.Font = New Font("Segoe UI", 9, If(s.Item2 = "Unassigned", FontStyle.Underline, FontStyle.Regular))
            lblClass.ForeColor = If(s.Item2 = "Unassigned", Color.FromArgb(150, 130, 240), Color.FromArgb(160, 168, 190))
            lblClass.AutoSize = True
            lblClass.Location = New Point(colX(1), rowY)
            lblClass.Cursor = Cursors.Hand
            Dim studentName = s.Item1
            AddHandler lblClass.Click, Async Sub() Await HandleSetClassClickAsync(studentName)
            panel.Controls.Add(lblClass)

            Dim lblCompleted As New Label()
            lblCompleted.Text = s.Item3
            lblCompleted.Font = New Font("Segoe UI", 9)
            lblCompleted.ForeColor = Color.FromArgb(160, 168, 190)
            lblCompleted.AutoSize = True
            lblCompleted.Location = New Point(colX(2), rowY)
            panel.Controls.Add(lblCompleted)

            Dim lblAverage As New Label()
            lblAverage.Text = s.Item4
            lblAverage.Font = New Font("Segoe UI", 9, FontStyle.Bold)
            lblAverage.ForeColor = Color.White
            lblAverage.AutoSize = True
            lblAverage.Location = New Point(colX(3), rowY)
            panel.Controls.Add(lblAverage)

            Dim badge As New Label()
            badge.Text = "  " & s.Item5 & "  "
            badge.Font = New Font("Segoe UI", 7.75, FontStyle.Bold)
            badge.AutoSize = True
            badge.Location = New Point(colX(4), rowY - 3)
            If isInLab Then
                badge.BackColor = Color.FromArgb(20, 60, 46)
                badge.ForeColor = Color.FromArgb(120, 220, 170)
            Else
                badge.BackColor = Color.FromArgb(30, 34, 56)
                badge.ForeColor = Color.FromArgb(150, 158, 180)
            End If
            panel.Controls.Add(badge)

            rowY += 40
        Next
    End Sub

    ' ---------- Average by class (bar chart) ----------

    Private Sub BuildAverageByClassPanel()
        Dim gap As Integer = 24
        Dim totalW As Integer = content.Width - 72
        Dim leftW As Integer = CInt(totalW * 0.58)
        Dim rightW As Integer = totalW - leftW - gap
        Dim rightX As Integer = 36 + leftW + gap
        Dim panelY As Integer = 212
        Dim panelH As Integer = 208

        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(rightX, panelY)
        panel.Size = New Size(rightW, panelH)
        panel.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        content.Controls.Add(panel)

        Dim lblTitle As New Label()
        lblTitle.Text = "📊  Average by class"
        lblTitle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(20, 18)
        panel.Controls.Add(lblTitle)

        Dim chart As New Panel()
        chart.Location = New Point(16, 52)
        chart.Size = New Size(rightW - 32, panelH - 68)
        chart.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        AddHandler chart.Paint, AddressOf DrawBarChart
        panel.Controls.Add(chart)
    End Sub

    Private Sub DrawBarChart(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim chart As Panel = DirectCast(sender, Panel)
        Dim w As Integer = chart.Width
        Dim h As Integer = chart.Height

        Dim axisLeft As Integer = 28
        Dim axisBottom As Integer = h - 20
        Dim plotTop As Integer = 6
        Dim plotHeight As Integer = axisBottom - plotTop

        ' Gridlines + axis labels (0 / 25 / 50 / 75 / 100)
        Using gridPen As New Pen(Color.FromArgb(28, 32, 52), 1)
            Using f As New Font("Segoe UI", 7)
                For i As Integer = 0 To 4
                    Dim val As Integer = i * 25
                    Dim gy As Single = axisBottom - (val / 100.0F) * plotHeight
                    g.DrawLine(gridPen, axisLeft, gy, w, gy)
                    Dim sf As New StringFormat With {.Alignment = StringAlignment.Far, .LineAlignment = StringAlignment.Center}
                    g.DrawString(val.ToString(), f, New SolidBrush(Color.FromArgb(110, 118, 140)), New RectangleF(0, gy - 7, axisLeft - 6, 14), sf)
                Next
            End Using
        End Using

        ' Bars
        Dim plotWidth As Integer = w - axisLeft - 8
        Dim n As Integer = classAverages.Length
        Dim slot As Single = plotWidth / CSng(n)
        Dim barWidth As Single = Math.Min(38.0F, slot * 0.5F)

        Using f As New Font("Segoe UI", 7.5)
            For i As Integer = 0 To n - 1
                Dim label = classAverages(i).Item1
                Dim value = classAverages(i).Item2
                Dim barH As Single = (value / 100.0F) * plotHeight
                Dim cx As Single = axisLeft + slot * i + slot / 2.0F
                Dim barX As Single = cx - barWidth / 2.0F
                Dim barY As Single = axisBottom - barH

                Using path = TopRoundedRectPath(New RectangleF(barX, barY, barWidth, barH), 6)
                    Using br As New SolidBrush(Color.FromArgb(45, 212, 191))
                        g.FillPath(br, path)
                    End Using
                End Using

                Dim sf As New StringFormat With {.Alignment = StringAlignment.Center}
                g.DrawString(label, f, New SolidBrush(Color.FromArgb(150, 158, 180)), New RectangleF(cx - slot / 2.0F, axisBottom + 4, slot, 16), sf)
            Next
        End Using
    End Sub

    ''' <summary>Rectangle path rounded only at the top-left/top-right corners, used for bar chart bars.</summary>
    Private Function TopRoundedRectPath(bounds As RectangleF, radius As Single) As GraphicsPath
        Dim path As New GraphicsPath()
        If bounds.Width <= 0 OrElse bounds.Height <= 0 Then Return path
        Dim r As Single = Math.Min(radius, Math.Min(bounds.Width / 2.0F, bounds.Height))
        If r <= 0.5F Then
            path.AddRectangle(bounds)
            Return path
        End If
        Dim d As Single = r * 2.0F
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90)
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90)
        path.AddLine(bounds.Right, bounds.Y + r, bounds.Right, bounds.Bottom)
        path.AddLine(bounds.Right, bounds.Bottom, bounds.X, bounds.Bottom)
        path.AddLine(bounds.X, bounds.Bottom, bounds.X, bounds.Y + r)
        path.CloseFigure()
        Return path
    End Function

    ' ---------- Grading queue ----------

    Private Sub BuildGradingQueuePanel()
        Dim gap As Integer = 24
        Dim totalW As Integer = content.Width - 72
        Dim leftW As Integer = CInt(totalW * 0.58)
        Dim rightW As Integer = totalW - leftW - gap
        Dim rightX As Integer = 36 + leftW + gap
        Dim panelY As Integer = 212 + 208 + 16
        Dim panelH As Integer = 156

        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(rightX, panelY)
        panel.Size = New Size(rightW, panelH)
        panel.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        content.Controls.Add(panel)

        Dim lblTitle As New Label()
        lblTitle.Text = "🕐  Grading queue"
        lblTitle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(20, 16)
        panel.Controls.Add(lblTitle)

        Dim rowY As Integer = 50
        For Each item In gradingQueue
            Dim assessmentId = item.Item1
            Dim title = item.Item2
            Dim studentName = item.Item3

            Dim lblItem As New Label()
            lblItem.Text = title & " — " & studentName
            lblItem.Font = New Font("Segoe UI", 9)
            lblItem.ForeColor = Color.FromArgb(200, 206, 224)
            lblItem.AutoSize = True
            lblItem.Location = New Point(20, rowY)
            lblItem.MaximumSize = New Size(rightW - 100, 0)
            panel.Controls.Add(lblItem)

            Dim lblGrade As New Label()
            lblGrade.Text = "⟳ Grade"
            lblGrade.Font = New Font("Segoe UI", 8.5, FontStyle.Bold)
            lblGrade.ForeColor = Color.FromArgb(150, 130, 240)
            lblGrade.AutoSize = True
            lblGrade.Cursor = Cursors.Hand
            lblGrade.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            lblGrade.Location = New Point(rightW - 20 - TextRenderer.MeasureText("⟳ Grade", lblGrade.Font).Width, rowY - 1)
            AddHandler lblGrade.Click, Async Sub() Await HandleGradeClickAsync(assessmentId, title, studentName)
            panel.Controls.Add(lblGrade)

            rowY += 34
        Next
    End Sub

    ''' <summary>
    ''' Prompts for a score and marks the assessment Graded. Offline fallback
    ''' rows (assessmentId = 0) just show the old demo message since there's no
    ''' real database row behind them.
    ''' </summary>
    Private Async Function HandleGradeClickAsync(assessmentId As Integer, title As String, studentName As String) As Task
        If assessmentId <= 0 Then
            MessageBox.Show($"Opens grading for '{title}' — {studentName} (demo — not connected to the database).", "ChemLab Virtual")
            Return
        End If

        Dim input = Microsoft.VisualBasic.Interaction.InputBox(
            $"Enter a score (0-100) for '{title}' — {studentName}:", "Grade assessment", "")
        If String.IsNullOrWhiteSpace(input) Then Return ' cancelled

        Dim score As Integer
        If Not Integer.TryParse(input.Trim(), score) OrElse score < 0 OrElse score > 100 Then
            MessageBox.Show("Please enter a whole number between 0 and 100.", "Grade assessment", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Await TeacherRepository.GradeAssessmentAsync(assessmentId, score, userName)
            Await LoadFromDbAsync(Nothing, EventArgs.Empty)
        Catch ex As Exception
            MessageBox.Show($"Couldn't save this grade: {ex.Message}", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    ''' <summary>Lets a teacher click any student's class ("Unassigned" or otherwise) and set it via a quick prompt.</summary>
    Private Async Function HandleSetClassClickAsync(studentName As String) As Task
        Dim input = Microsoft.VisualBasic.Interaction.InputBox(
            $"Set {studentName}'s class (e.g. ""Grade 11-B""). Leave blank to unassign:", "Set class", "")

        ' InputBox returns "" both for "left blank on purpose" and "clicked Cancel" —
        ' there's no reliable way to tell those apart, so treat blank as "no change"
        ' rather than risk silently un-assigning a class the teacher didn't mean to clear.
        If input Is Nothing Then Return
        If input.Trim().Length = 0 Then Return

        Try
            Dim studentId = Await UsersRepository.FindUserIdByDisplayNameAsync(studentName)
            If Not studentId.HasValue Then
                MessageBox.Show($"Couldn't find {studentName} in the database (this row may be offline fallback data).", "ChemLab Virtual")
                Return
            End If
            Await TeacherRepository.SetStudentClassAsync(studentId.Value, input.Trim(), userName)
            Await LoadFromDbAsync(Nothing, EventArgs.Empty)
        Catch ex As Exception
            MessageBox.Show($"Couldn't update this student's class: {ex.Message}", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

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
        DrawNavIcon(g, "cap", Color.White)
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