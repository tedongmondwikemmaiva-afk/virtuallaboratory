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
        sidebar = New Panel()
        sidebar.Dock = DockStyle.Left
        sidebar.Width = 244
        sidebar.BackColor = Color.FromArgb(12, 15, 30)
        Me.Controls.Add(sidebar)

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

        Dim navItems As (String, Boolean)() = {
            ("Overview", True), ("Students", False), ("Teachers", False), ("Experiments Library", False),
            ("Reports", False), ("System Settings", False)
        }
        Dim y As Integer = 90
        For Each item In navItems
            CreateNavItem(item.Item1, item.Item2, y)
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
            Dim handler As EventHandler = Sub() MessageBox.Show($"'{label}' is coming soon in a future update.", "ChemLab Admin")
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

End Class
