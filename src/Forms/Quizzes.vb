Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows.Forms

' Quizzes screen: sidebar + titlebar (same chrome as HomeForm/ApparatusForm) plus a
' quiz card (badge, countdown timer, progress bar, two questions per page with
' selectable answers, Previous/Next Question controls) and a "Your scores" side
' panel, matching the "Quizzes" screenshot.
Public Class Quizzes
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

    ' ----- quiz data -----
    ' Public so QuizzesRepository (in src/Data) can build and return these directly.
    Public Class QuizQuestion
        Public Property QuestionId As Integer         ' 0 for the offline fallback questions
        Public Property Text As String
        Public Property Options As String()
        Public Property OptionIds As Integer()         ' parallel to Options(); 0 where not backed by the DB
        Public Property CorrectIndex As Integer
        Public Property Selected As Integer = -1
    End Class

    ' The quiz currently shown is always the single "Acids & Bases — Quiz 1"
    ' seeded as quiz_id = 1. Extend this (and the sidebar) once there's more
    ' than one quiz to choose from.
    Private Const QuizId As Integer = 1

    ' Offline fallback shown immediately; LoadQuizFromDbAsync() (fired from
    ' Form.Load) replaces this with the real questions from `quiz_questions`/
    ' `quiz_options` and re-renders once they arrive.
    Private questions As New List(Of QuizQuestion) From {
        New QuizQuestion With {
            .Text = "1. What products are formed when HCl reacts with NaOH?",
            .Options = {"Salt and water", "Hydrogen gas only", "Carbon dioxide and water", "No reaction"},
            .CorrectIndex = 0,
            .Selected = 0
        },
        New QuizQuestion With {
            .Text = "2. The pH at the equivalence point of a strong acid–strong base titration is:",
            .Options = {"Below 3", "Exactly 7", "Above 11", "Depends on volume"},
            .CorrectIndex = 1
        },
        New QuizQuestion With {
            .Text = "3. Which indicator is commonly used for a strong acid–strong base titration?",
            .Options = {"Phenolphthalein", "Starch", "Litmus paper only", "None needed"},
            .CorrectIndex = 0
        },
        New QuizQuestion With {
            .Text = "4. What type of reaction occurs between HCl and NaOH?",
            .Options = {"Neutralization", "Oxidation", "Precipitation", "Combustion"},
            .CorrectIndex = 0
        }
    }

    ' subject, score % — shown in the right-hand "Your scores" panel.
    ' Offline fallback; replaced with real per-topic mastery from the database
    ' (reusing the same `mastery_topics` table Reports & Grades reads from).
    Private scores As New List(Of (Subject As String, Percent As Integer)) From {
        ("Acids & Bases", 92),
        ("Reactions", 78),
        ("Solutions", 85),
        ("Analysis", 68)
    }
    Private scoreValueLabels As New Dictionary(Of String, Label)

    ' Resolved against the database in LoadQuizFromDbAsync; falls back to 1
    ' (the seeded demo user) until then. Swap in real session/user-id
    ' management once you have it instead of relying on this.
    Private currentUserId As Integer = 1

    Private Const QuestionsPerPage As Integer = 2
    Private currentPage As Integer = 0
    Private totalPages As Integer

    Private quizCard As RoundedPanel
    Private questionsHost As Panel
    Private progressFill As Panel
    Private progressTrack As Panel
    Private lblTimer As Label
    Private btnPrevious As DarkButton
    Private btnNext As GradientButton

    Private remainingSeconds As Integer = 4 * 60 + 32 ' 04:32, matches the screenshot
    Private countdownTimer As Timer

    Public Sub New(displayName As String, role As String)
        userName = If(String.IsNullOrWhiteSpace(displayName), "Student", displayName)
        userRole = If(String.IsNullOrWhiteSpace(role), "Student", role)
        totalPages = CInt(Math.Ceiling(questions.Count / CDbl(QuestionsPerPage)))

        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1200, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.AutoScroll = True
        Me.BackColor = Color.FromArgb(9, 12, 24)
        Me.Text = "ChemLab Virtual — Quizzes"

        BuildTitleBar()
        BuildSidebar()
        BuildContent()

        ' Reflow the dashboard content when the window is maximized/restored so
        ' cards and panels use the available width instead of staying stuck at
        ' the size they were first built at. BuildContent() rebuilds the quiz
        ' card from scratch, so re-render the current page afterwards too —
        ' otherwise the questions area would come back empty after a resize.
        AddHandler Me.Resize, Sub()
                                   If Me.WindowState <> FormWindowState.Minimized Then
                                       BuildContent()
                                       RenderPage(currentPage)
                                   End If
                               End Sub
        RenderPage(0)
        StartCountdown()

        AddHandler Me.Load, AddressOf LoadQuizFromDbAsync

        AddHandler Me.FormClosed, Sub()
                                       If countdownTimer IsNot Nothing Then
                                           countdownTimer.Stop()
                                           countdownTimer.Dispose()
                                       End If
                                   End Sub
    End Sub

    ' ===================== TITLE BAR =====================

    Private Sub BuildTitleBar()
        Dim titleBar As New Panel()
        titleBar.Dock = DockStyle.Top
        titleBar.Height = 40
        titleBar.BackColor = Color.FromArgb(9, 12, 24)
        Me.Controls.Add(titleBar)

        Dim lblTitle As New Label() With {.Text = "ChemLab Virtual — Quizzes", .Font = New Font("Segoe UI", 9.5, FontStyle.Regular),
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

    ' ===================== SIDEBAR (same nav as HomeForm, "Quizzes" active) =====================

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
            ("question", "Quizzes", True),
            ("chart", "Reports && Grades", False),
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
                                                   Case "chart"
                                                       Try
                                                           NavigateToForm(New ReportsGrades(userName, userRole))
                                                       Catch ex As Exception
                                                           MessageBox.Show($"Failed to open Reports & Grades: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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

        Dim lblTitle As New Label() With {.Text = "Quizzes", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(36, 28)}
        content.Controls.Add(lblTitle)

        Dim lblSub As New Label() With {.Text = "Check your understanding after each experiment.",
                                         .Font = New Font("Segoe UI", 10.5), .ForeColor = Color.FromArgb(140, 148, 210), .AutoSize = True, .Location = New Point(36, 62)}
        content.Controls.Add(lblSub)

        Dim rightWidth As Integer = 260
        Dim gap As Integer = 20
        Dim quizWidth As Integer = content.Width - 72 - rightWidth - gap

        BuildQuizCard(36, 104, quizWidth)
        BuildScoresPanel(36 + quizWidth + gap, 104, rightWidth)
        BuildBottomToolbar()
    End Sub

    ' ----- quiz card (badge, timer, progress bar, questions, nav buttons) -----

    Private Sub BuildQuizCard(x As Integer, y As Integer, w As Integer)
        quizCard = New RoundedPanel()
        quizCard.CornerRadius = 14
        quizCard.FillColor = Color.FromArgb(16, 20, 40)
        quizCard.BorderColor = Color.FromArgb(36, 41, 66)
        quizCard.Location = New Point(x, y)
        quizCard.Size = New Size(w, content.Height - y - 100)
        quizCard.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        content.Controls.Add(quizCard)

        Dim badge As New Label() With {.Text = "🎓  Acids & Bases — Quiz 1", .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                                        .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, 18), .BackColor = Color.Transparent}
        quizCard.Controls.Add(badge)

        lblTimer = New Label() With {.Text = "⏱  " & FormatTime(remainingSeconds) & " left", .Font = New Font("Segoe UI", 8.5, FontStyle.Bold),
                                      .ForeColor = Color.FromArgb(230, 150, 70), .BackColor = Color.FromArgb(45, 32, 16),
                                      .AutoSize = True, .Padding = New Padding(10, 4, 10, 4)}
        lblTimer.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblTimer.Location = New Point(w - 20 - lblTimer.PreferredWidth, 16)
        quizCard.Controls.Add(lblTimer)

        ' progress bar
        progressTrack = New RoundedPanel() With {.CornerRadius = 3, .FillColor = Color.FromArgb(30, 34, 56), .BorderColor = Color.FromArgb(30, 34, 56), .DrawBorder = False}
        progressTrack.Location = New Point(20, 54)
        progressTrack.Size = New Size(w - 40, 6)
        progressTrack.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        quizCard.Controls.Add(progressTrack)

        progressFill = New RoundedPanel() With {.CornerRadius = 3, .DrawBorder = False}
        progressFill.Location = New Point(0, 0)
        progressFill.Size = New Size(0, 6)
        AddHandler progressFill.Paint, Sub(s, e)
                                            Dim g = e.Graphics
                                            g.SmoothingMode = SmoothingMode.AntiAlias
                                            Dim rect As New Rectangle(0, 0, progressFill.Width, progressFill.Height)
                                            If rect.Width < 1 Then Return
                                            Using path = RoundedRectPath(rect, 3)
                                                Using br As New LinearGradientBrush(rect, Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205), 0.0F)
                                                    g.FillPath(br, path)
                                                End Using
                                            End Using
                                        End Sub
        progressTrack.Controls.Add(progressFill)

        ' scrollable host for the two questions on the current page
        questionsHost = New Panel()
        questionsHost.Location = New Point(0, 76)
        questionsHost.Size = New Size(w, quizCard.Height - 76 - 64)
        questionsHost.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        questionsHost.AutoScroll = True
        quizCard.Controls.Add(questionsHost)

        ' Previous / Next Question buttons
        btnPrevious = New DarkButton() With {.Text = "Previous", .Size = New Size(110, 38)}
        btnPrevious.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        btnPrevious.Location = New Point(20, quizCard.Height - 54)
        AddHandler btnPrevious.Click, Sub() GoToPage(currentPage - 1)
        quizCard.Controls.Add(btnPrevious)

        btnNext = New GradientButton() With {.Text = "Next Question", .Size = New Size(150, 38)}
        btnNext.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnNext.Location = New Point(w - 20 - btnNext.Width, quizCard.Height - 54)
        AddHandler btnNext.Click, Sub() GoToPage(currentPage + 1)
        quizCard.Controls.Add(btnNext)

        AddHandler quizCard.Resize, Sub()
                                         lblTimer.Location = New Point(quizCard.Width - 20 - lblTimer.PreferredWidth, 16)
                                         btnPrevious.Location = New Point(20, quizCard.Height - 54)
                                         btnNext.Location = New Point(quizCard.Width - 20 - btnNext.Width, quizCard.Height - 54)
                                         UpdateProgressBar()
                                     End Sub
    End Sub

    Private Sub GoToPage(pageIndex As Integer)
        If pageIndex < 0 OrElse pageIndex >= totalPages Then
            If pageIndex >= totalPages Then FinishQuiz()
            Return
        End If
        RenderPage(pageIndex)
    End Sub

    Private Sub RenderPage(pageIndex As Integer)
        currentPage = pageIndex
        questionsHost.Controls.Clear()

        Dim startIndex As Integer = currentPage * QuestionsPerPage
        Dim endIndex As Integer = Math.Min(startIndex + QuestionsPerPage - 1, questions.Count - 1)

        Dim qy As Integer = 4
        For i As Integer = startIndex To endIndex
            qy = BuildQuestionBlock(questions(i), i, 20, qy, questionsHost.Width - 40) + 18
        Next

        btnPrevious.Enabled = currentPage > 0
        btnNext.Text = If(currentPage >= totalPages - 1, "Finish Quiz", "Next Question")
        UpdateProgressBar()
    End Sub

    ' builds one question block (question text + 2x2 answer grid) and returns the Y
    ' coordinate immediately below it, so the caller can stack the next question
    Private Function BuildQuestionBlock(q As QuizQuestion, questionIndex As Integer, x As Integer, y As Integer, w As Integer) As Integer
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 12
        panel.FillColor = Color.FromArgb(13, 16, 32)
        panel.BorderColor = Color.FromArgb(30, 34, 56)
        panel.Location = New Point(x, y)
        panel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right

        Dim lblQ As New Label() With {.Text = q.Text, .Font = New Font("Segoe UI", 10.5, FontStyle.Bold), .ForeColor = Color.White,
                                       .Location = New Point(16, 14), .Size = New Size(w - 32, 40), .BackColor = Color.Transparent}
        panel.Controls.Add(lblQ)

        Dim optW As Integer = (w - 32 - 12) \ 2
        Dim optH As Integer = 38
        Dim optionPanels As New List(Of RoundedPanel)

        For i As Integer = 0 To q.Options.Length - 1
            Dim col As Integer = i Mod 2
            Dim row As Integer = i \ 2
            Dim ox As Integer = 16 + col * (optW + 12)
            Dim oy As Integer = 58 + row * (optH + 10)

            Dim opt As New RoundedPanel()
            opt.CornerRadius = 8
            opt.Size = New Size(optW, optH)
            opt.Location = New Point(ox, oy)
            opt.Cursor = Cursors.Hand
            optionPanels.Add(opt)

            Dim lblOpt As New Label() With {.Text = q.Options(i), .Font = New Font("Segoe UI", 9.5), .AutoSize = False,
                                             .Location = New Point(12, 0), .Size = New Size(optW - 34, optH), .TextAlign = ContentAlignment.MiddleLeft,
                                             .BackColor = Color.Transparent}
            opt.Controls.Add(lblOpt)

            Dim lblCheck As New Label() With {.Text = "✓", .Font = New Font("Segoe UI", 10, FontStyle.Bold), .AutoSize = False,
                                               .Size = New Size(22, optH), .Location = New Point(optW - 26, 0), .TextAlign = ContentAlignment.MiddleCenter,
                                               .BackColor = Color.Transparent, .ForeColor = Color.White, .Visible = (q.Selected = i)}
            opt.Controls.Add(lblCheck)

            Dim optIndex As Integer = i
            Dim clickHandler As EventHandler = Sub()
                                                    q.Selected = optIndex
                                                    RestyleOptions(q, optionPanels)
                                                    RecalculateAcidsAndBasesScore()
                                                End Sub
            AddHandler opt.Click, clickHandler
            AddHandler lblOpt.Click, clickHandler

            panel.Controls.Add(opt)
        Next

        RestyleOptions(q, optionPanels)

        Dim rows As Integer = CInt(Math.Ceiling(q.Options.Length / 2.0))
        panel.Size = New Size(w, 58 + rows * (optH + 10) + 6)
        questionsHost.Controls.Add(panel)

        Return y + panel.Height
    End Function

    Private Sub RestyleOptions(q As QuizQuestion, optionPanels As List(Of RoundedPanel))
        For i As Integer = 0 To optionPanels.Count - 1
            Dim opt = optionPanels(i)
            Dim isSelected As Boolean = (q.Selected = i)
            If isSelected Then
                opt.FillColor = Color.FromArgb(108, 92, 231)
                opt.BorderColor = Color.FromArgb(108, 92, 231)
            Else
                opt.FillColor = Color.FromArgb(18, 22, 42)
                opt.BorderColor = Color.FromArgb(40, 45, 70)
            End If
            For Each c As Control In opt.Controls
                If TypeOf c Is Label Then
                    Dim lbl = DirectCast(c, Label)
                    If lbl.Text = "✓" Then
                        lbl.Visible = isSelected
                    Else
                        lbl.ForeColor = If(isSelected, Color.White, Color.FromArgb(190, 196, 216))
                        lbl.Font = New Font("Segoe UI", 9.5, If(isSelected, FontStyle.Bold, FontStyle.Regular))
                    End If
                End If
            Next
            opt.Invalidate()
        Next
    End Sub

    Private Sub UpdateProgressBar()
        If progressTrack Is Nothing OrElse progressFill Is Nothing Then Return
        Dim answered As Integer = questions.Where(Function(q) q.Selected >= 0).Count()
        Dim ratio As Double = If(questions.Count = 0, 0, answered / CDbl(questions.Count))
        Dim fillW As Integer = CInt(progressTrack.Width * ratio)
        progressFill.Size = New Size(Math.Max(fillW, If(ratio > 0, 6, 0)), progressTrack.Height)
        progressFill.Invalidate()
    End Sub

    Private quizSubject As String = "Acids & Bases"

    Private Sub RecalculateAcidsAndBasesScore()
        Dim answered = questions.Where(Function(q) q.Selected >= 0).ToList()
        If answered.Count = 0 Then Return
        Dim correct = answered.Where(Function(q) q.Selected = q.CorrectIndex).Count()
        Dim percent = CInt(Math.Round(correct / CDbl(answered.Count) * 100))
        UpdateScore(quizSubject, percent)
        UpdateProgressBar()
    End Sub

    ''' <summary>
    ''' Loads the real quiz + questions from the database (falling back to the
    ''' offline sample above if unreachable), and loads the "Your scores" panel
    ''' from mastery_topics for the signed-in user.
    ''' </summary>
    Private Async Sub LoadQuizFromDbAsync(sender As Object, e As EventArgs)
        Try
            Dim resolvedId = Await UsersRepository.FindUserIdByDisplayNameAsync(userName)
            If resolvedId.HasValue Then currentUserId = resolvedId.Value

            Dim quizTask = QuizzesRepository.GetQuizWithQuestionsAsync(QuizId)
            Dim scoresTask = ReportsRepository.GetMasteryTopicsAsync(currentUserId)
            Await Task.WhenAll(quizTask, scoresTask)

            If quizTask.Result.Questions.Count > 0 Then
                quizSubject = quizTask.Result.Subject
                questions = quizTask.Result.Questions
                totalPages = CInt(Math.Ceiling(questions.Count / CDbl(QuestionsPerPage)))
                currentPage = 0
            End If
            If scoresTask.Result.Count > 0 Then
                scores = scoresTask.Result.Select(Function(t) (t.Topic, CInt(t.Percent))).ToList()
            End If

            BuildContent()
            RenderPage(0)
        Catch ex As Exception
            Debug.WriteLine($"Could not load quiz from database: {ex.Message}")
        End Try
    End Sub

    Private Async Sub FinishQuiz()
        Dim answered = questions.Where(Function(q) q.Selected >= 0).ToList()
        Dim correct = answered.Where(Function(q) q.Selected = q.CorrectIndex).Count()
        Dim percent = If(answered.Count = 0, 0, CInt(Math.Round(correct / CDbl(answered.Count) * 100)))

        Try
            ' Only persist if the questions actually came from the database
            ' (QuestionId = 0 means we're still on the offline fallback set).
            If questions.Count > 0 AndAlso questions(0).QuestionId > 0 Then
                Await QuizzesRepository.SaveAttemptAsync(QuizId, currentUserId, percent, questions)
                Await UsersRepository.LogActivityAsync(currentUserId, "quiz_submitted", $"Scored {percent}% on '{quizSubject}'")
            End If
        Catch ex As Exception
            Debug.WriteLine($"Could not save quiz attempt: {ex.Message}")
        End Try

        MessageBox.Show($"Quiz complete! Your {quizSubject} score has been updated ({percent}%).", "ChemLab Virtual", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Function FormatTime(totalSeconds As Integer) As String
        Dim m As Integer = totalSeconds \ 60
        Dim s As Integer = totalSeconds Mod 60
        Return $"{m:00}:{s:00}"
    End Function

    Private Sub StartCountdown()
        countdownTimer = New Timer()
        countdownTimer.Interval = 1000
        AddHandler countdownTimer.Tick, Sub()
                                             If remainingSeconds <= 0 Then
                                                 countdownTimer.Stop()
                                                 lblTimer.Text = "⏱  Time's up"
                                                 Return
                                             End If
                                             remainingSeconds -= 1
                                             lblTimer.Text = "⏱  " & FormatTime(remainingSeconds) & " left"
                                             lblTimer.Location = New Point(quizCard.Width - 20 - lblTimer.PreferredWidth, 16)
                                         End Sub
        countdownTimer.Start()
    End Sub

    ' ----- "Your scores" panel -----

    Private Sub BuildScoresPanel(x As Integer, y As Integer, w As Integer)
        Dim panel As New RoundedPanel()
        panel.CornerRadius = 14
        panel.FillColor = Color.FromArgb(16, 20, 40)
        panel.BorderColor = Color.FromArgb(36, 41, 66)
        panel.Location = New Point(x, y)
        panel.Size = New Size(w, content.Height - y - 100)
        panel.Anchor = AnchorStyles.Top Or AnchorStyles.Right Or AnchorStyles.Bottom
        content.Controls.Add(panel)

        Dim lblTitle As New Label() With {.Text = "🎓  Your scores", .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                                           .ForeColor = Color.White, .AutoSize = True, .Location = New Point(18, 18), .BackColor = Color.Transparent}
        panel.Controls.Add(lblTitle)

        Dim rowY As Integer = 58
        For Each sc In scores
            Dim lblSubject As New Label() With {.Text = sc.Subject, .Font = New Font("Segoe UI", 9.5, FontStyle.Bold),
                                                 .ForeColor = Color.FromArgb(150, 170, 255), .AutoSize = True, .Location = New Point(18, rowY)}
            panel.Controls.Add(lblSubject)

            Dim lblPercent As New Label() With {.Text = sc.Percent & "%", .Font = New Font("Segoe UI", 8.5, FontStyle.Bold),
                                                 .ForeColor = Color.FromArgb(120, 220, 170), .BackColor = Color.FromArgb(18, 42, 34),
                                                 .AutoSize = True, .Padding = New Padding(8, 3, 8, 3)}
            lblPercent.Location = New Point(w - 20 - lblPercent.PreferredWidth, rowY - 3)
            panel.Controls.Add(lblPercent)
            scoreValueLabels(sc.Subject) = lblPercent

            rowY += 46
        Next
    End Sub

    Private Sub UpdateScore(subject As String, percent As Integer)
        Dim idx = scores.FindIndex(Function(s) s.Subject = subject)
        If idx < 0 Then Return
        scores(idx) = (subject, percent)
        If scoreValueLabels.ContainsKey(subject) Then
            Dim lbl = scoreValueLabels(subject)
            lbl.Text = percent & "%"
            lbl.ForeColor = If(percent >= 70, Color.FromArgb(120, 220, 170), Color.FromArgb(220, 140, 120))
            lbl.BackColor = If(percent >= 70, Color.FromArgb(18, 42, 34), Color.FromArgb(46, 26, 24))
        End If
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