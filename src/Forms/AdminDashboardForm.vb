Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class AdminDashboardForm
    Inherits Form

    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HT_CAPTION As Integer = &H2

    Private ReadOnly adminName As String
    Private sidebar As Panel
    Private content As Panel
    Private currentPage As String = "Overview"

    Public Sub New(displayName As String)
        adminName = If(String.IsNullOrWhiteSpace(displayName), "Administrator", displayName)

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1200, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.AutoScroll = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Admin"

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
    ''' Loads real recent-activity rows and pending-teacher approvals from the
    ''' database, replacing the offline fallback and re-rendering. Silently
    ''' keeps the fallback if the database isn't reachable.
    ''' </summary>
    Private Async Sub LoadFromDbAsync(sender As Object, e As EventArgs)
        Try
            Dim activityTask = AdminRepository.GetRecentActivityAsync(5)
            Dim pendingTask = AdminRepository.GetPendingTeachersAsync()
            Await Task.WhenAll(activityTask, pendingTask)

            If activityTask.Result.Count > 0 Then
                activityRows = activityTask.Result.Select(Function(a) (a.Who, a.What, a.WhenText)).ToArray()
            End If
            ' Pending teachers legitimately can be empty (nothing awaiting approval),
            ' so replace unconditionally rather than only when non-empty.
            pendingTeachers = pendingTask.Result.Select(Function(p) (p.UserId, p.DisplayLine)).ToArray()

            BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not load admin dashboard data from database: {ex.Message}")
        End Try
    End Sub

    Private Sub BuildTitleBar()
        Dim titleBar As New Panel()
        titleBar.Dock = DockStyle.Top
        titleBar.Height = 40
        titleBar.BackColor = Color.FromArgb(9, 12, 24)
        Me.Controls.Add(titleBar)

        Dim lblTitle As New Label()
        lblTitle.Text = "ChemLab Virtual — Admin"
        lblTitle.Font = New Font("Segoe UI", 9.5)
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

        Dim btnClose As New Label() With {.Text = "✕", .Size = New Size(40, 40), .TextAlign = ContentAlignment.MiddleCenter,
                                           .ForeColor = Color.FromArgb(160, 168, 190), .Cursor = Cursors.Hand, .Dock = DockStyle.Right}
        AddHandler btnClose.Click, Sub() Me.Close()
        AddHandler btnClose.MouseEnter, Sub() btnClose.BackColor = Color.FromArgb(200, 60, 60)
        AddHandler btnClose.MouseLeave, Sub() btnClose.BackColor = Color.Transparent

        Dim btnMax As New Label() With {.Text = "☐", .Size = New Size(40, 40), .TextAlign = ContentAlignment.MiddleCenter,
                                         .ForeColor = Color.FromArgb(160, 168, 190), .Cursor = Cursors.Hand, .Dock = DockStyle.Right}
        AddHandler btnMax.Click, Sub() Me.WindowState = If(Me.WindowState = FormWindowState.Maximized, FormWindowState.Normal, FormWindowState.Maximized)
        AddHandler btnMax.MouseEnter, Sub() btnMax.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMax.MouseLeave, Sub() btnMax.BackColor = Color.Transparent

        Dim btnMin As New Label() With {.Text = "—", .Size = New Size(40, 40), .TextAlign = ContentAlignment.MiddleCenter,
                                         .ForeColor = Color.FromArgb(160, 168, 190), .Cursor = Cursors.Hand, .Dock = DockStyle.Right}
        AddHandler btnMin.Click, Sub() Me.WindowState = FormWindowState.Minimized
        AddHandler btnMin.MouseEnter, Sub() btnMin.BackColor = Color.FromArgb(30, 34, 56)
        AddHandler btnMin.MouseLeave, Sub() btnMin.BackColor = Color.Transparent

        titleBar.Controls.Add(btnClose)
        titleBar.Controls.Add(btnMax)
        titleBar.Controls.Add(btnMin)
    End Sub

    Private Sub BuildSidebar()
        If sidebar Is Nothing Then
            sidebar = New Panel()
            sidebar.Dock = DockStyle.Left
            sidebar.Width = 244
            sidebar.BackColor = Color.FromArgb(12, 15, 30)
            Me.Controls.Add(sidebar)
        Else
            ' Re-entering to reflect a page change: wipe and rebuild so the
            ' active-item highlight matches currentPage.
            While sidebar.Controls.Count > 0
                sidebar.Controls(0).Dispose()
            End While
        End If

        Dim iconBox As New Panel()
        iconBox.Size = New Size(44, 44)
        iconBox.Location = New Point(20, 20)
        AddHandler iconBox.Paint, Sub(s, e)
                                       Dim g = e.Graphics
                                       g.SmoothingMode = SmoothingMode.AntiAlias
                                       Dim rect As New Rectangle(0, 0, 43, 43)
                                       Using path = RoundedRectPath(rect, 12)
                                           Using br As New LinearGradientBrush(rect, Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205), 45.0F)
                                               g.FillPath(br, path)
                                           End Using
                                       End Using
                                       Using pen As New Pen(Color.White, 1.8F)
                                           g.DrawLine(pen, 19, 13, 19, 20) : g.DrawLine(pen, 25, 13, 25, 20)
                                           g.DrawLine(pen, 19, 20, 12, 32) : g.DrawLine(pen, 25, 20, 32, 32)
                                           g.DrawLine(pen, 12, 32, 32, 32)
                                       End Using
                                   End Sub
        sidebar.Controls.Add(iconBox)

        Dim lblChem As New Label() With {.Text = "ChemLab", .Font = New Font("Segoe UI", 13, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(74, 20)}
        Dim lblVirtual As New Label() With {.Text = "A D M I N", .Font = New Font("Segoe UI", 7.5), .ForeColor = Color.FromArgb(150, 158, 185), .AutoSize = True, .Location = New Point(75, 43)}
        sidebar.Controls.Add(lblChem)
        sidebar.Controls.Add(lblVirtual)

        Dim navItems As String() = {"Overview", "Students", "Teachers", "Experiments Library", "Reports", "System Settings"}
        Dim y As Integer = 90
        For Each label In navItems
            CreateNavItem(label, label = currentPage, y)
            y += 46
        Next

        Dim footer As New Panel()
        footer.Size = New Size(sidebar.Width, 64)
        footer.Location = New Point(0, sidebar.Height - 64)
        footer.Anchor = AnchorStyles.Left Or AnchorStyles.Bottom Or AnchorStyles.Right
        footer.BackColor = Color.FromArgb(14, 17, 34)
        sidebar.Controls.Add(footer)

        Dim avatar As New Panel() With {.Size = New Size(38, 38), .Location = New Point(20, 13)}
        AddHandler avatar.Paint, Sub(s, e)
                                      Dim g = e.Graphics
                                      g.SmoothingMode = SmoothingMode.AntiAlias
                                      Dim rect As New Rectangle(0, 0, 37, 37)
                                      Using br As New LinearGradientBrush(rect, Color.FromArgb(214, 82, 205), Color.FromArgb(108, 92, 231), 45.0F)
                                          g.FillEllipse(br, rect)
                                      End Using
                                      Dim initials = If(adminName.Length >= 2, adminName.Substring(0, 2).ToUpper(), adminName.ToUpper())
                                      Using f As New Font("Segoe UI", 9, FontStyle.Bold)
                                          Dim sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                                          g.DrawString(initials, f, Brushes.White, rect, sf)
                                      End Using
                                  End Sub
        footer.Controls.Add(avatar)

        Dim lblName As New Label() With {.Text = adminName, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(68, 12)}
        Dim lblRole As New Label() With {.Text = "Administrator", .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(140, 148, 170), .AutoSize = True, .Location = New Point(68, 32)}
        footer.Controls.Add(lblName)
        footer.Controls.Add(lblRole)

        Dim btnLogout As New Label() With {.Text = "⏻", .Font = New Font("Segoe UI", 12), .ForeColor = Color.FromArgb(150, 158, 185),
                                            .Size = New Size(30, 30), .TextAlign = ContentAlignment.MiddleCenter, .Location = New Point(sidebar.Width - 46, 17), .Cursor = Cursors.Hand}
        AddHandler btnLogout.Click, Sub()
                                        If MessageBox.Show("Log out of the admin dashboard?", "Log out", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                                            Me.DialogResult = DialogResult.Retry
                                            Me.Close()
                                        End If
                                    End Sub
        footer.Controls.Add(btnLogout)
    End Sub

    Private Sub CreateNavItem(label As String, isActive As Boolean, y As Integer)
        Dim item As New RoundedPanel()
        item.CornerRadius = 10
        item.Size = New Size(sidebar.Width - 32, 40)
        item.Location = New Point(16, y)
        item.Cursor = Cursors.Hand
        item.FillColor = If(isActive, Color.FromArgb(108, 92, 231), sidebar.BackColor)
        item.BorderColor = item.FillColor

        Dim lbl As New Label()
        lbl.Text = label
        lbl.Font = New Font("Segoe UI", 10, If(isActive, FontStyle.Bold, FontStyle.Regular))
        lbl.ForeColor = If(isActive, Color.White, Color.FromArgb(180, 188, 208))
        lbl.AutoSize = True
        lbl.Location = New Point(18, 10)
        item.Controls.Add(lbl)

        If Not isActive Then
            Dim handler As EventHandler = Sub() NavigateTo(label)
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

    ' Offline fallback rows shown immediately; LoadFromDbAsync() (fired from
    ' Form.Load) replaces these with real data and re-renders once it arrives.
    Private activityRows As (String, String, String)() = {
        ("Amara Okafor", "Completed 'Titration' with 94%", "2 min ago"),
        ("Mr. Daniels (Teacher)", "Published a new experiment: 'Redox Reactions'", "18 min ago"),
        ("Liam Chen", "Started 'Gas Evolution'", "42 min ago"),
        ("New signup", "Priya Nair registered as a Student", "1 hr ago"),
        ("System", "Weekly usage report generated", "3 hr ago")
    }
    ' UserId, DisplayLine — real rows carry a real user_id so Approve/Deny can
    ' act on them; the offline fallback uses -1 since there's nothing to update.
    Private pendingTeachers As (UserId As Integer, DisplayLine As String)() = {
        (-1, "Dr. Sarah Whitfield — Chemistry Dept."),
        (-1, "Mr. Ben Okoro — Physical Science")
    }

    Private Async Sub NavigateTo(page As String)
        currentPage = page
        BuildSidebar()
        BuildContent()
        ' Students/Teachers/Reports need real data; load it after switching so
        ' the page paints immediately and fills in once the query returns.
        Select Case page
            Case "Students" : Await LoadStudentsAsync()
            Case "Teachers" : Await LoadTeachersAsync()
            Case "Experiments Library" : Await LoadExperimentsAsync()
            Case "Reports" : Await LoadReportsAsync()
            Case "System Settings" : Await LoadSettingsAsync()
        End Select
    End Sub

    Private Sub BuildContent()
        If content Is Nothing Then
            content = New Panel()
            content.Dock = DockStyle.Fill
            content.BackColor = Color.FromArgb(9, 12, 24)
            content.AutoScroll = True
            Me.Controls.Add(content)
            Me.Controls.SetChildIndex(content, 0)
        Else
            ' Re-entering BuildContent (e.g. on resize/maximize, or a page switch):
            ' wipe the previous controls and rebuild against the new content size.
            While content.Controls.Count > 0
                content.Controls(0).Dispose()
            End While
        End If

        Select Case currentPage
            Case "Overview" : BuildOverviewPage()
            Case "Students" : BuildStudentsPage()
            Case "Teachers" : BuildTeachersPage()
            Case "Experiments Library" : BuildExperimentsPage()
            Case "Reports" : BuildReportsPage()
            Case "System Settings" : BuildSystemSettingsPage()
            Case Else : BuildNotBuiltYetPage(currentPage)
        End Select
    End Sub

    Private Sub BuildOverviewPage()
        Dim lblWelcome As New Label() With {.Text = $"Welcome back, {adminName}", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        Dim lblSub As New Label() With {.Text = "Here's what's happening across ChemLab Virtual today.", .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblWelcome)
        content.Controls.Add(lblSub)

        Dim stats As (String, String, Color, Color)() = {
            ("312", "Total students", Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205)),
            ("14", "Active teachers", Color.FromArgb(92, 130, 231), Color.FromArgb(120, 200, 231)),
            ("1,204", "Experiments run this month", Color.FromArgb(150, 92, 231), Color.FromArgb(214, 82, 170)),
            ("47", "Currently active sessions", Color.FromArgb(92, 150, 231), Color.FromArgb(180, 92, 231))
        }
        Dim statGap As Integer = 20
        Dim statWidth As Integer = (content.Width - 72 - statGap * 3) \ 4
        For i As Integer = 0 To stats.Length - 1
            CreateStatCard(stats(i).Item1, stats(i).Item2, stats(i).Item3, stats(i).Item4, 36 + i * (statWidth + statGap), 106, statWidth)
        Next

        BuildActivityPanel(36, 220, content.Width - 72, 320)
        BuildApprovalsPanel(36, 556, content.Width - 72, 200)
    End Sub

    Private Sub CreateStatCard(value As String, label As String, c1 As Color, c2 As Color, x As Integer, y As Integer, w As Integer)
        Dim card As New RoundedPanel()
        card.CornerRadius = 14
        card.FillColor = Color.FromArgb(16, 20, 40)
        card.BorderColor = Color.FromArgb(36, 41, 66)
        card.Location = New Point(x, y)
        card.Size = New Size(w, 88)
        content.Controls.Add(card)

        Dim dot As New Panel() With {.Size = New Size(10, 10), .Location = New Point(18, 20)}
        AddHandler dot.Paint, Sub(s, e)
                                   Using br As New LinearGradientBrush(New Rectangle(0, 0, 9, 9), c1, c2, 45.0F)
                                       e.Graphics.FillEllipse(br, 0, 0, 9, 9)
                                   End Using
                               End Sub
        card.Controls.Add(dot)

        Dim lblValue As New Label() With {.Text = value, .Font = New Font("Segoe UI", 17, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(18, 34)}
        Dim lblLabel As New Label() With {.Text = label, .Font = New Font("Segoe UI", 9), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(18, 64), .MaximumSize = New Size(w - 36, 0)}
        card.Controls.Add(lblValue)
        card.Controls.Add(lblLabel)
    End Sub

    Private Sub BuildActivityPanel(x As Integer, y As Integer, w As Integer, h As Integer)
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        content.Controls.Add(panel)

        Dim lblTitle As New Label() With {.Text = "📋  Recent activity", .Font = New Font("Segoe UI", 11, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, 18)}
        panel.Controls.Add(lblTitle)

        Dim rowY As Integer = 58
        For Each r In activityRows
            Dim lblWho As New Label() With {.Text = r.Item1, .Font = New Font("Segoe UI", 9.5, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, rowY)}
            Dim lblWhat As New Label() With {.Text = r.Item2, .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(20, rowY + 18)}
            Dim lblWhen As New Label() With {.Text = r.Item3, .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(110, 118, 140), .AutoSize = True, .Location = New Point(w - 100, rowY + 6)}
            panel.Controls.Add(lblWho)
            panel.Controls.Add(lblWhat)
            panel.Controls.Add(lblWhen)
            rowY += 44
        Next
    End Sub

    Private Sub BuildApprovalsPanel(x As Integer, y As Integer, w As Integer, h As Integer)
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        content.Controls.Add(panel)

        Dim lblTitle As New Label() With {.Text = "⏳  Pending teacher approvals", .Font = New Font("Segoe UI", 11, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, 18)}
        panel.Controls.Add(lblTitle)

        If pendingTeachers.Length = 0 Then
            Dim lblNone As New Label() With {.Text = "No teacher accounts are waiting for approval.", .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(20, 58)}
            panel.Controls.Add(lblNone)
            Return
        End If

        Dim rowY As Integer = 58
        For Each t In pendingTeachers
            Dim userId = t.UserId
            Dim displayLine = t.DisplayLine

            Dim lblName As New Label() With {.Text = displayLine, .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, rowY + 10)}
            panel.Controls.Add(lblName)

            Dim btnApprove As New GradientButton() With {.Text = "Approve", .Size = New Size(96, 34), .Location = New Point(w - 220, rowY)}
            AddHandler btnApprove.Click, Async Sub() Await HandleApprovalAsync(userId, displayLine, approve:=True)
            panel.Controls.Add(btnApprove)

            Dim btnDeny As New DarkButton() With {.Text = "Deny", .Size = New Size(96, 34), .Location = New Point(w - 116, rowY)}
            AddHandler btnDeny.Click, Async Sub() Await HandleApprovalAsync(userId, displayLine, approve:=False)
            panel.Controls.Add(btnDeny)

            rowY += 54
        Next
    End Sub

    ''' <summary>Approves/denies a pending teacher account and refreshes the panel.
    ''' If this is offline fallback data (userId = -1, no real database row), just
    ''' shows the old demo message instead of trying to update anything.</summary>
    Private Async Function HandleApprovalAsync(userId As Integer, displayLine As String, approve As Boolean) As Task
        If userId < 0 Then
            MessageBox.Show($"{displayLine} {If(approve, "approved", "denied")} (demo — not connected to the database).", "ChemLab Admin")
            Return
        End If

        Try
            If approve Then
                Await AdminRepository.ApproveTeacherAsync(userId, adminName)
            Else
                Await AdminRepository.DenyTeacherAsync(userId, adminName)
            End If
            MessageBox.Show($"{displayLine} {If(approve, "approved", "denied")}.", "ChemLab Admin")
            Await LoadPendingAndRebuildAsync()
        Catch ex As Exception
            MessageBox.Show($"Couldn't update this account: {ex.Message}", "ChemLab Admin", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Async Function LoadPendingAndRebuildAsync() As Task
        Try
            Dim fresh = Await AdminRepository.GetPendingTeachersAsync()
            pendingTeachers = fresh.Select(Function(p) (p.UserId, p.DisplayLine)).ToArray()
            BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not refresh pending teachers: {ex.Message}")
        End Try
    End Function

    ' ===================== Students page =====================

    Private students As New List(Of AdminRepository.StudentDto)

    Private Async Function LoadStudentsAsync() As Task
        Try
            students = Await AdminRepository.GetAllStudentsAsync()
            If currentPage = "Students" Then BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not load students: {ex.Message}")
            If currentPage = "Students" Then ShowLoadErrorBanner("students")
        End Try
    End Function

    Private Sub BuildStudentsPage()
        Dim lblTitle As New Label() With {.Text = "Students", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        Dim lblSub As New Label() With {.Text = $"{students.Count} registered student account(s).", .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblTitle)
        content.Controls.Add(lblSub)

        If students.Count = 0 Then
            AddEmptyOrLoadingLabel("Loading students…")
            Return
        End If

        Dim panel = BeginListPanel(36, 106, content.Width - 72, students.Count * 54 + 20)
        Dim rowY As Integer = 16
        For Each s In students
            Dim lblName As New Label() With {.Text = s.DisplayName, .Font = New Font("Segoe UI", 9.5, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, rowY)}
            Dim lblMeta As New Label() With {.Text = $"{s.Email}  ·  joined {s.JoinedText}  ·  last login {s.LastLoginText}", .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(20, rowY + 18)}
            panel.Controls.Add(lblName)
            panel.Controls.Add(lblMeta)

            Dim userId = s.UserId
            Dim isActive = s.IsActive
            Dim btnToggle As New DarkButton() With {.Text = If(isActive, "Deactivate", "Reactivate"), .Size = New Size(110, 32), .Location = New Point(panel.Width - 130, rowY)}
            AddHandler btnToggle.Click, Async Sub()
                                             Try
                                                 Await AdminRepository.SetStudentActiveAsync(userId, Not isActive, adminName)
                                                 Await LoadStudentsAsync()
                                             Catch ex As Exception
                                                 MessageBox.Show($"Couldn't update this account: {ex.Message}", "ChemLab Admin", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                             End Try
                                         End Sub
            panel.Controls.Add(btnToggle)

            rowY += 54
        Next
    End Sub

    ' ===================== Teachers page =====================

    Private teachers As New List(Of AdminRepository.TeacherDto)

    Private Async Function LoadTeachersAsync() As Task
        Try
            teachers = Await AdminRepository.GetAllTeachersAsync()
            If currentPage = "Teachers" Then BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not load teachers: {ex.Message}")
            If currentPage = "Teachers" Then ShowLoadErrorBanner("teachers")
        End Try
    End Function

    Private Sub BuildTeachersPage()
        Dim lblTitle As New Label() With {.Text = "Teachers", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        Dim lblSub As New Label() With {.Text = $"{teachers.Count} teacher account(s) — approve, deny, or revoke access here.", .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblTitle)
        content.Controls.Add(lblSub)

        If teachers.Count = 0 Then
            AddEmptyOrLoadingLabel("Loading teachers…")
            Return
        End If

        Dim panel = BeginListPanel(36, 106, content.Width - 72, teachers.Count * 54 + 20)
        Dim rowY As Integer = 16
        For Each t In teachers
            Dim lblName As New Label() With {.Text = t.DisplayName, .Font = New Font("Segoe UI", 9.5, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, rowY)}
            Dim lblMeta As New Label() With {.Text = $"{t.Email}  ·  joined {t.JoinedText}", .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(20, rowY + 18)}
            panel.Controls.Add(lblName)
            panel.Controls.Add(lblMeta)

            Dim statusColor = If(t.ApprovalStatus = "Approved", Color.FromArgb(120, 220, 170),
                              If(t.ApprovalStatus = "Pending", Color.FromArgb(230, 170, 100), Color.FromArgb(220, 100, 100)))
            Dim lblStatus As New Label() With {.Text = t.ApprovalStatus, .Font = New Font("Segoe UI", 8.5, FontStyle.Bold), .ForeColor = statusColor, .AutoSize = True, .Location = New Point(panel.Width - 330, rowY + 8)}
            panel.Controls.Add(lblStatus)

            Dim userId = t.UserId
            Dim displayName = t.DisplayName
            If t.ApprovalStatus = "Pending" Then
                Dim btnApprove As New GradientButton() With {.Text = "Approve", .Size = New Size(90, 32), .Location = New Point(panel.Width - 216, rowY)}
                AddHandler btnApprove.Click, Async Sub() Await HandleTeacherActionAsync(userId, displayName, approve:=True)
                panel.Controls.Add(btnApprove)

                Dim btnDeny As New DarkButton() With {.Text = "Deny", .Size = New Size(90, 32), .Location = New Point(panel.Width - 116, rowY)}
                AddHandler btnDeny.Click, Async Sub() Await HandleTeacherActionAsync(userId, displayName, approve:=False)
                panel.Controls.Add(btnDeny)
            Else
                Dim btnRevoke As New DarkButton() With {.Text = If(t.ApprovalStatus = "Approved", "Revoke", "Reconsider"), .Size = New Size(120, 32), .Location = New Point(panel.Width - 140, rowY)}
                Dim approveNow = (t.ApprovalStatus <> "Approved")
                AddHandler btnRevoke.Click, Async Sub() Await HandleTeacherActionAsync(userId, displayName, approve:=approveNow)
                panel.Controls.Add(btnRevoke)
            End If

            rowY += 54
        Next
    End Sub

    Private Async Function HandleTeacherActionAsync(userId As Integer, displayName As String, approve As Boolean) As Task
        Try
            If approve Then
                Await AdminRepository.ApproveTeacherAsync(userId, adminName)
            Else
                Await AdminRepository.DenyTeacherAsync(userId, adminName)
            End If
            Await LoadTeachersAsync()
        Catch ex As Exception
            MessageBox.Show($"Couldn't update {displayName}: {ex.Message}", "ChemLab Admin", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    ' ===================== Reports page =====================

    Private platformStats As AdminRepository.PlatformStatsDto
    Private topStudents As New List(Of AdminRepository.TopStudentDto)

    Private Async Function LoadReportsAsync() As Task
        Try
            Dim statsTask = AdminRepository.GetPlatformStatsAsync()
            Dim topTask = AdminRepository.GetTopStudentsAsync(5)
            Await Task.WhenAll(statsTask, topTask)
            platformStats = statsTask.Result
            topStudents = topTask.Result
            If currentPage = "Reports" Then BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not load reports: {ex.Message}")
            If currentPage = "Reports" Then ShowLoadErrorBanner("platform reports")
        End Try
    End Function

    Private Sub BuildReportsPage()
        Dim lblTitle As New Label() With {.Text = "Reports", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        Dim lblSub As New Label() With {.Text = "Platform-wide activity, pulled live from the database.", .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblTitle)
        content.Controls.Add(lblSub)

        If platformStats Is Nothing Then
            AddEmptyOrLoadingLabel("Loading reports…")
            Return
        End If

        Dim stats As (String, String, Color, Color)() = {
            (platformStats.TotalStudents.ToString(), "Total students", Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205)),
            (platformStats.TotalTeachers.ToString(), "Approved teachers", Color.FromArgb(92, 130, 231), Color.FromArgb(120, 200, 231)),
            (platformStats.TotalQuizAttempts.ToString(), "Quiz attempts submitted", Color.FromArgb(150, 92, 231), Color.FromArgb(214, 82, 170)),
            ($"{Math.Round(platformStats.AverageQuizScore, 1)}%", "Average quiz score", Color.FromArgb(92, 150, 231), Color.FromArgb(180, 92, 231))
        }
        Dim statGap As Integer = 20
        Dim statWidth As Integer = (content.Width - 72 - statGap * 3) \ 4
        For i As Integer = 0 To stats.Length - 1
            CreateStatCard(stats(i).Item1, stats(i).Item2, stats(i).Item3, stats(i).Item4, 36 + i * (statWidth + statGap), 106, statWidth)
        Next

        Dim lblExtra As New Label() With {
            .Text = $"{platformStats.PendingTeachers} teacher account(s) awaiting approval  ·  {platformStats.TotalAssessments} graded assessment(s), averaging {Math.Round(platformStats.AverageAssessmentScore, 1)}%",
            .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(36, 210)}
        content.Controls.Add(lblExtra)

        Dim panelH = Math.Max(80, topStudents.Count * 44 + 60)
        Dim panel = BeginListPanel(36, 246, content.Width - 72, panelH)
        Dim lblPanelTitle As New Label() With {.Text = "Top students by average score", .Font = New Font("Segoe UI", 11, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, 16)}
        panel.Controls.Add(lblPanelTitle)

        If topStudents.Count = 0 Then
            Dim lblNone As New Label() With {.Text = "No graded assessments yet.", .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(20, 54)}
            panel.Controls.Add(lblNone)
        Else
            Dim rowY As Integer = 54
            For Each t In topStudents
                Dim lblName As New Label() With {.Text = t.DisplayName, .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, rowY)}
                Dim lblScore As New Label() With {.Text = $"{Math.Round(t.AverageScore, 1)}% avg over {t.AssessmentCount} assessment(s)", .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(panel.Width - 260, rowY + 1)}
                panel.Controls.Add(lblName)
                panel.Controls.Add(lblScore)
                rowY += 36
            Next
        End If
    End Sub

    ' ===================== Experiments Library page =====================

    Private experiments As New List(Of ExperimentsRepository.ExperimentDto)

    Private Async Function LoadExperimentsAsync() As Task
        Try
            experiments = Await ExperimentsRepository.GetAllAsync()
            If currentPage = "Experiments Library" Then BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not load experiments: {ex.Message}")
            If currentPage = "Experiments Library" Then ShowLoadErrorBanner("the experiments library")
        End Try
    End Function

    Private Sub BuildExperimentsPage()
        Dim lblTitle As New Label() With {.Text = "Experiments Library", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        Dim lblSub As New Label() With {.Text = $"{experiments.Count} experiment(s) — draft, publish, archive, or remove.", .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblTitle)
        content.Controls.Add(lblSub)

        Dim btnAdd As New GradientButton() With {.Text = "+ Add experiment", .Size = New Size(160, 36), .Location = New Point(content.Width - 196, 30), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
        AddHandler btnAdd.Click, AddressOf HandleAddExperimentAsync
        content.Controls.Add(btnAdd)

        If experiments.Count = 0 Then
            AddEmptyOrLoadingLabel("Loading experiments… (or none exist yet — click ""+ Add experiment"" to create the first one)")
            Return
        End If

        Dim panel = BeginListPanel(36, 106, content.Width - 72, experiments.Count * 68 + 20)
        Dim rowY As Integer = 16
        For Each exp In experiments
            Dim statusColor = If(exp.Status = "Published", Color.FromArgb(120, 220, 170),
                              If(exp.Status = "Draft", Color.FromArgb(230, 170, 100), Color.FromArgb(150, 158, 180)))

            Dim lblTitleRow As New Label() With {.Text = exp.Title, .Font = New Font("Segoe UI", 9.5, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, rowY)}
            Dim lblMeta As New Label() With {
                .Text = $"{exp.Category}  ·  {exp.Difficulty}  ·  ~{exp.EstDurationMinutes} min  ·  by {exp.AuthorName}  ·  {exp.CompletionCount} completed",
                .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(20, rowY + 18)}
            Dim lblStatus As New Label() With {.Text = exp.Status, .Font = New Font("Segoe UI", 8.5, FontStyle.Bold), .ForeColor = statusColor, .AutoSize = True, .Location = New Point(20, rowY + 38)}
            panel.Controls.Add(lblTitleRow)
            panel.Controls.Add(lblMeta)
            panel.Controls.Add(lblStatus)

            Dim expId = exp.ExperimentId
            Dim title = exp.Title
            Dim status = exp.Status

            Dim btnX As Integer = panel.Width - 300
            If status <> "Published" Then
                Dim btnPublish As New GradientButton() With {.Text = "Publish", .Size = New Size(90, 32), .Location = New Point(btnX, rowY + 12)}
                AddHandler btnPublish.Click, Async Sub() Await HandleExperimentStatusAsync(expId, "Published", title)
                panel.Controls.Add(btnPublish)
                btnX += 98
            End If
            If status <> "Archived" Then
                Dim btnArchive As New DarkButton() With {.Text = "Archive", .Size = New Size(90, 32), .Location = New Point(btnX, rowY + 12)}
                AddHandler btnArchive.Click, Async Sub() Await HandleExperimentStatusAsync(expId, "Archived", title)
                panel.Controls.Add(btnArchive)
                btnX += 98
            End If
            Dim btnDelete As New DarkButton() With {.Text = "Delete", .FillColor = Color.FromArgb(60, 24, 28), .BorderColor = Color.FromArgb(120, 40, 46), .Size = New Size(90, 32), .Location = New Point(btnX, rowY + 12)}
            AddHandler btnDelete.Click, Async Sub() Await HandleExperimentDeleteAsync(expId, title)
            panel.Controls.Add(btnDelete)

            rowY += 68
        Next
    End Sub

    Private Async Sub HandleAddExperimentAsync(sender As Object, e As EventArgs)
        Dim title = Microsoft.VisualBasic.Interaction.InputBox("Experiment title:", "Add experiment", "")
        If String.IsNullOrWhiteSpace(title) Then Return

        Dim description = Microsoft.VisualBasic.Interaction.InputBox("Short description:", "Add experiment", "")
        If String.IsNullOrWhiteSpace(description) Then description = "(No description provided.)"

        Dim category = Microsoft.VisualBasic.Interaction.InputBox("Category (e.g. ""Acids & Bases"", ""Redox""):", "Add experiment", "General")
        If String.IsNullOrWhiteSpace(category) Then category = "General"

        Dim difficulty = Microsoft.VisualBasic.Interaction.InputBox("Difficulty — type Beginner, Intermediate, or Advanced:", "Add experiment", "Beginner")
        If Not {"Beginner", "Intermediate", "Advanced"}.Contains(difficulty.Trim()) Then difficulty = "Beginner"

        Dim durationText = Microsoft.VisualBasic.Interaction.InputBox("Estimated duration in minutes:", "Add experiment", "30")
        Dim duration As Integer
        If Not Integer.TryParse(durationText, duration) OrElse duration <= 0 Then duration = 30

        Try
            Dim authorId = Await UsersRepository.FindUserIdByDisplayNameAsync(adminName)
            Await ExperimentsRepository.CreateAsync(title.Trim(), description.Trim(), category.Trim(), difficulty.Trim(), duration, authorId)
            Await LoadExperimentsAsync()
        Catch ex As Exception
            MessageBox.Show($"Couldn't create this experiment: {ex.Message}", "ChemLab Admin", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Function HandleExperimentStatusAsync(experimentId As Integer, newStatus As String, title As String) As Task
        Try
            Await ExperimentsRepository.SetStatusAsync(experimentId, newStatus, adminName)
            Await LoadExperimentsAsync()
        Catch ex As Exception
            MessageBox.Show($"Couldn't update '{title}': {ex.Message}", "ChemLab Admin", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Async Function HandleExperimentDeleteAsync(experimentId As Integer, title As String) As Task
        Dim confirm = MessageBox.Show($"Delete '{title}' permanently? This can't be undone.", "Delete experiment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
        If confirm <> DialogResult.Yes Then Return
        Try
            Await ExperimentsRepository.DeleteAsync(experimentId, adminName)
            Await LoadExperimentsAsync()
        Catch ex As Exception
            MessageBox.Show($"Couldn't delete '{title}': {ex.Message}", "ChemLab Admin", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    ' ===================== System Settings page =====================

    Private settings As New Dictionary(Of String, String)

    Private Async Function LoadSettingsAsync() As Task
        Try
            settings = Await SettingsRepository.GetAllAsync()
            If currentPage = "System Settings" Then BuildContent()
        Catch ex As Exception
            Debug.WriteLine($"Could not load settings: {ex.Message}")
            If currentPage = "System Settings" Then ShowLoadErrorBanner("system settings")
        End Try
    End Function

    Private Sub BuildSystemSettingsPage()
        Dim lblTitle As New Label() With {.Text = "System Settings", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        Dim lblSub As New Label() With {.Text = "These control real application behavior — changes apply immediately.", .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblTitle)
        content.Controls.Add(lblSub)

        If settings.Count = 0 Then
            AddEmptyOrLoadingLabel("Loading settings…")
            Return
        End If

        Dim panel = BeginListPanel(36, 106, Math.Min(680, content.Width - 72), 320)

        Dim siteName = If(settings.ContainsKey("site_name"), settings("site_name"), "ChemLab Virtual")
        Dim lblSiteLabel As New Label() With {.Text = "Site name", .Font = New Font("Segoe UI", 9.5, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, 20)}
        Dim lblSiteValue As New Label() With {.Text = siteName, .Font = New Font("Segoe UI", 9), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(20, 40), .Cursor = Cursors.Hand}
        AddHandler lblSiteValue.Click, AddressOf HandleEditSiteNameAsync
        panel.Controls.Add(lblSiteLabel)
        panel.Controls.Add(lblSiteValue)
        Dim btnEditSite As New DarkButton() With {.Text = "Edit", .Size = New Size(80, 30), .Location = New Point(panel.Width - 100, 20)}
        AddHandler btnEditSite.Click, AddressOf HandleEditSiteNameAsync
        panel.Controls.Add(btnEditSite)

        AddSettingToggleRow(panel, 90, "allow_new_signups", "Allow new signups",
                             "When off, Create Account is blocked for everyone (Students and Teachers).")
        AddSettingToggleRow(panel, 160, "require_teacher_approval", "Require Teacher approval",
                             "When off, new Teacher signups are approved instantly instead of needing your review.")
        AddSettingToggleRow(panel, 230, "maintenance_mode", "Maintenance mode",
                             "When on, only Admin accounts can sign in — everyone else sees a maintenance message.")
    End Sub

    Private Sub AddSettingToggleRow(panel As RoundedPanel, rowY As Integer, key As String, label As String, description As String)
        Dim isOn = settings.ContainsKey(key) AndAlso settings(key).Trim().ToLowerInvariant() = "true"

        Dim lblLabel As New Label() With {.Text = label, .Font = New Font("Segoe UI", 9.5, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, rowY)}
        Dim lblDesc As New Label() With {.Text = description, .Font = New Font("Segoe UI", 8.5), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = False, .Size = New Size(panel.Width - 160, 34), .Location = New Point(20, rowY + 18)}
        panel.Controls.Add(lblLabel)
        panel.Controls.Add(lblDesc)

        Dim btnToggle As New GradientButton() With {.Text = If(isOn, "ON", "OFF"), .Size = New Size(80, 34), .Location = New Point(panel.Width - 100, rowY)}
        If Not isOn Then
            btnToggle.ColorStart = Color.FromArgb(50, 55, 80)
            btnToggle.ColorEnd = Color.FromArgb(40, 44, 66)
        End If
        AddHandler btnToggle.Click, Async Sub() Await HandleToggleSettingAsync(key, Not isOn)
        panel.Controls.Add(btnToggle)
    End Sub

    Private Async Function HandleToggleSettingAsync(key As String, newValue As Boolean) As Task
        Try
            Await SettingsRepository.SetAsync(key, newValue.ToString().ToLowerInvariant(), adminName)
            Await LoadSettingsAsync()
        Catch ex As Exception
            MessageBox.Show($"Couldn't update this setting: {ex.Message}", "ChemLab Admin", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Async Sub HandleEditSiteNameAsync(sender As Object, e As EventArgs)
        Dim current = If(settings.ContainsKey("site_name"), settings("site_name"), "ChemLab Virtual")
        Dim input = Microsoft.VisualBasic.Interaction.InputBox("Site name:", "Edit setting", current)
        If String.IsNullOrWhiteSpace(input) Then Return
        Try
            Await SettingsRepository.SetAsync("site_name", input.Trim(), adminName)
            Await LoadSettingsAsync()
        Catch ex As Exception
            MessageBox.Show($"Couldn't update this setting: {ex.Message}", "ChemLab Admin", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ===================== Pages without a data model yet =====================

    ''' <summary>
    ''' Defensive fallback only — every real nav item (Overview/Students/
    ''' Teachers/Experiments Library/Reports/System Settings) has its own
    ''' builder above. This only fires if currentPage somehow ends up as
    ''' something unexpected.
    ''' </summary>
    Private Sub BuildNotBuiltYetPage(pageName As String)
        Dim lblTitle As New Label() With {.Text = pageName, .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        content.Controls.Add(lblTitle)

        Dim panel = BeginListPanel(36, 90, Math.Min(560, content.Width - 72), 120)
        Dim lbl As New Label() With {
            .Text = $"'{pageName}' isn't a recognized page.",
            .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(160, 168, 190), .AutoSize = False,
            .Size = New Size(panel.Width - 40, 90), .Location = New Point(20, 16)}
        panel.Controls.Add(lbl)
    End Sub

    ' ===================== Shared list-page helpers =====================

    Private Function BeginListPanel(x As Integer, y As Integer, w As Integer, h As Integer) As RoundedPanel
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, h)
        content.Controls.Add(panel)
        Return panel
    End Function

    Private Sub AddEmptyOrLoadingLabel(text As String)
        Dim lbl As New Label() With {.Text = text, .Font = New Font("Segoe UI", 10), .ForeColor = Color.FromArgb(150, 158, 180), .AutoSize = True, .Location = New Point(36, 106)}
        content.Controls.Add(lbl)
    End Sub

    Private Sub ShowLoadErrorBanner(what As String)
        Dim lbl As New Label() With {.Text = $"Couldn't load {what} from the database. Check your connection and try again.", .Font = New Font("Segoe UI", 9.5), .ForeColor = Color.FromArgb(220, 140, 140), .AutoSize = True, .Location = New Point(36, 106)}
        content.Controls.Add(lbl)
    End Sub

End Class
