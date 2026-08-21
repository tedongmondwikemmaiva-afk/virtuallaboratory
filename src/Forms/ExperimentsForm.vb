Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows.Forms

''' <summary>
''' "Experiments" screen: browse published experiments from the Experiments
''' Library, start them, and mark them complete. Mirrors the sidebar/title-bar
''' chrome used by the other screens so it feels like the same app.
''' </summary>
Public Class ExperimentsForm
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
    Private currentUserId As Integer?

    Private sidebar As Panel
    Private titleBar As Panel
    Private content As Panel

    ' Offline fallback shown immediately; LoadFromDbAsync() (fired from Form.Load)
    ' replaces this with the real published experiments once they arrive.
    Private experiments As New List(Of ExperimentsRepository.ExperimentDto) From {
        New ExperimentsRepository.ExperimentDto With {
            .ExperimentId = 0, .Title = "Acid-Base Titration",
            .Description = "Determine the concentration of an unknown HCl solution by titrating against standardized NaOH.",
            .Category = "Acids & Bases", .Difficulty = "Beginner", .EstDurationMinutes = 40,
            .Status = "Published", .AuthorName = "Your teacher", .CreatedText = "", .CompletionCount = 0
        },
        New ExperimentsRepository.ExperimentDto With {
            .ExperimentId = 0, .Title = "Precipitation Reactions",
            .Description = "React silver nitrate with sodium chloride and observe the formation of an insoluble precipitate.",
            .Category = "Reactions", .Difficulty = "Beginner", .EstDurationMinutes = 25,
            .Status = "Published", .AuthorName = "Your teacher", .CreatedText = "", .CompletionCount = 0
        }
    }
    Private progress As New Dictionary(Of Integer, Boolean) ' experiment_id -> is complete

    Public Sub New(Optional displayName As String = "Mac Falen", Optional role As String = "Student")
        userName = If(String.IsNullOrWhiteSpace(displayName), "Student", displayName)
        userRole = If(String.IsNullOrWhiteSpace(role), "Student", role)

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1200, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.AutoScroll = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Experiments"

        BuildTitleBar()
        BuildSidebar()
        BuildContent()

        AddHandler Me.Resize, Sub()
                                   If Me.WindowState <> FormWindowState.Minimized Then
                                       BuildContent()
                                   End If
                               End Sub

        AddHandler Me.Load, Sub(s, e) LoadFromDbAsync()
    End Sub

    ''' <summary>
    ''' Replaces the offline fallback list with the real Published experiments
    ''' from the database, plus this student's start/completion progress.
    ''' Silently keeps the fallback if the database isn't reachable.
    ''' </summary>
    Private Async Sub LoadFromDbAsync()
        Try
            currentUserId = Await UsersRepository.FindUserIdByDisplayNameAsync(userName)

            Dim fromDb = Await ExperimentsRepository.GetPublishedAsync()
            If fromDb.Count > 0 Then experiments = fromDb

            If currentUserId.HasValue Then
                progress = Await ExperimentsRepository.GetProgressForUserAsync(currentUserId.Value)
            End If

            BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not load experiments from database: {ex.Message}")
        End Try
    End Sub

    ' ===================== TITLE BAR =====================

    Private Sub BuildTitleBar()
        titleBar = New Panel()
        titleBar.Dock = DockStyle.Top
        titleBar.Height = 40
        titleBar.BackColor = Color.FromArgb(9, 12, 24)
        Me.Controls.Add(titleBar)

        Dim lblTitle As New Label()
        lblTitle.Text = "ChemLab Virtual — Experiments"
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

        Dim navItems As New List(Of (String, String, Boolean)) From {
            ("home", "Home", False),
            ("flask", "Lab Workspace", False),
            ("book", "Experiments", True),
            ("grid", "Apparatus", False),
            ("beaker", "Chemicals", False),
            ("notebook", "Lab Notebook", False),
            ("question", "Quizzes", False),
            ("chart", "Reports && Grades", False),
            ("shield", "Safety Data", False),
            ("gear", "Settings", False)
        }

        ' Role-gated: only Teachers and Admins get a link into the Teacher
        ' Dashboard. Students never see it in their sidebar at all.
        If userRole = "Teacher" OrElse userRole = "Admin" Then
            navItems.Insert(navItems.Count - 1, ("cap", "Teacher Dashboard", False))
        End If

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
            ' Already on the Experiments screen — nothing to do.
        ElseIf iconKey = "home" Then
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
        ElseIf iconKey = "cap" Then
            Dim openTeacher As EventHandler = Sub()
                                                 Try
                                                     NavigateToForm(New TeacherDashboardForm(userName, userRole))
                                                 Catch ex As Exception
                                                     MessageBox.Show($"Failed to open Teacher Dashboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                 End Try
                                             End Sub
            AddHandler item.Click, openTeacher
            AddHandler lbl.Click, openTeacher
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

    ' ===================== CONTENT =====================

    Private Sub BuildContent()
        If content Is Nothing Then
            content = New Panel()
            content.Dock = DockStyle.Fill
            content.BackColor = Color.FromArgb(9, 12, 24)
            content.AutoScroll = True
            Me.Controls.Add(content)
            Me.Controls.SetChildIndex(content, 0)
        Else
            While content.Controls.Count > 0
                content.Controls(0).Dispose()
            End While
        End If

        Dim lblTitle As New Label()
        lblTitle.Text = "Experiments"
        lblTitle.Font = New Font("Segoe UI", 22, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(36, 28)
        content.Controls.Add(lblTitle)

        Dim lblSub As New Label()
        lblSub.Text = $"{experiments.Count} experiment(s) published by your teacher."
        lblSub.Font = New Font("Segoe UI", 10.5)
        lblSub.ForeColor = Color.FromArgb(140, 148, 210)
        lblSub.AutoSize = True
        lblSub.Location = New Point(36, 62)
        content.Controls.Add(lblSub)

        If experiments.Count = 0 Then
            Dim lblNone As New Label()
            lblNone.Text = "No experiments have been published yet — check back soon."
            lblNone.Font = New Font("Segoe UI", 10)
            lblNone.ForeColor = Color.FromArgb(150, 158, 180)
            lblNone.AutoSize = True
            lblNone.Location = New Point(36, 106)
            content.Controls.Add(lblNone)
            Return
        End If

        Dim gridWidth As Integer = Me.ClientSize.Width - sidebar.Width - 72
        Dim cardW As Integer = 340
        Dim cardH As Integer = 190
        Dim gap As Integer = 20
        Dim perRow As Integer = Math.Max(1, (gridWidth + gap) \ (cardW + gap))

        Dim i As Integer = 0
        For Each exp In experiments
            Dim col = i Mod perRow
            Dim row = i \ perRow
            Dim cx = 36 + col * (cardW + gap)
            Dim cy = 106 + row * (cardH + gap)
            BuildExperimentCard(exp, cx, cy, cardW, cardH)
            i += 1
        Next
    End Sub

    Private Sub BuildExperimentCard(exp As ExperimentsRepository.ExperimentDto, x As Integer, y As Integer, w As Integer, h As Integer)
        Dim card As New RoundedPanel()
        card.CornerRadius = 14
        card.FillColor = Color.FromArgb(16, 20, 40)
        card.BorderColor = Color.FromArgb(36, 41, 66)
        card.Location = New Point(x, y)
        card.Size = New Size(w, h)
        content.Controls.Add(card)

        Dim difficultyColor = If(exp.Difficulty = "Beginner", Color.FromArgb(120, 220, 170),
                              If(exp.Difficulty = "Intermediate", Color.FromArgb(230, 170, 100), Color.FromArgb(220, 100, 100)))
        Dim badge As New Label() With {.Text = "  " & exp.Difficulty & "  ", .Font = New Font("Segoe UI", 8, FontStyle.Bold), .ForeColor = difficultyColor, .BackColor = Color.FromArgb(20, 24, 44), .AutoSize = True, .Location = New Point(16, 16)}
        card.Controls.Add(badge)

        Dim lblCategory As New Label() With {.Text = exp.Category, .Font = New Font("Segoe UI", 8), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(w - 16 - TextRenderer.MeasureText(exp.Category, New Font("Segoe UI", 8)).Width, 20)}
        card.Controls.Add(lblCategory)

        Dim lblTitle As New Label() With {.Text = exp.Title, .Font = New Font("Segoe UI", 11.5, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = False, .Size = New Size(w - 32, 44), .Location = New Point(16, 44)}
        card.Controls.Add(lblTitle)

        Dim lblDesc As New Label() With {.Text = exp.Description, .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(160, 168, 190), .AutoSize = False, .Size = New Size(w - 32, 56), .Location = New Point(16, 84)}
        card.Controls.Add(lblDesc)

        Dim lblMeta As New Label() With {.Text = $"~{exp.EstDurationMinutes} min  ·  by {exp.AuthorName}", .Font = New Font("Segoe UI", 8), .ForeColor = Color.FromArgb(130, 138, 160), .AutoSize = True, .Location = New Point(16, h - 40)}
        card.Controls.Add(lblMeta)

        Dim isComplete = progress.ContainsKey(exp.ExperimentId) AndAlso progress(exp.ExperimentId)
        Dim isStarted = progress.ContainsKey(exp.ExperimentId)
        Dim expId = exp.ExperimentId
        Dim title = exp.Title

        If isComplete Then
            Dim lblDone As New Label() With {.Text = "✓ Completed", .Font = New Font("Segoe UI", 9, FontStyle.Bold), .ForeColor = Color.FromArgb(120, 220, 170), .AutoSize = True, .Location = New Point(16, h - 20)}
            card.Controls.Add(lblDone)
        Else
            Dim btn As New GradientButton() With {.Text = If(isStarted, "Mark complete", "Start experiment"), .Size = New Size(w - 32, 32), .Location = New Point(16, h - 42)}
            AddHandler btn.Click, Async Sub() Await HandleExperimentActionAsync(expId, title, isStarted)
            card.Controls.Add(btn)
        End If
    End Sub

    Private Async Function HandleExperimentActionAsync(experimentId As Integer, title As String, alreadyStarted As Boolean) As Task
        If experimentId <= 0 OrElse Not currentUserId.HasValue Then
            MessageBox.Show($"Opens '{title}' (demo — not connected to the database, or your account couldn't be matched).", "ChemLab Virtual")
            Return
        End If

        Try
            If alreadyStarted Then
                Await ExperimentsRepository.MarkCompletedAsync(experimentId, currentUserId.Value)
            Else
                Await ExperimentsRepository.MarkStartedAsync(experimentId, currentUserId.Value)
            End If
            progress = Await ExperimentsRepository.GetProgressForUserAsync(currentUserId.Value)
            BuildContent()
        Catch ex As Exception
            MessageBox.Show($"Couldn't update your progress: {ex.Message}", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
        DrawNavIcon(g, "book", Color.White)
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

    Private Function GetInitials(fullName As String) As String
        If String.IsNullOrWhiteSpace(fullName) Then Return "?"
        Dim parts = fullName.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        If parts.Length >= 2 Then Return (parts(0).Substring(0, 1) & parts(1).Substring(0, 1)).ToUpper()
        If parts.Length = 1 AndAlso parts(0).Length >= 2 Then Return parts(0).Substring(0, 2).ToUpper()
        If parts.Length = 1 Then Return parts(0).ToUpper()
        Return "?"
    End Function

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
