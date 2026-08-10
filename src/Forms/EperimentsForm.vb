Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

' ============================================================================
'  ChemLab Virtual - Experiments screen
'  Single-file VB.NET WinForms implementation.
'  Combines: Program (entry point), MainForm, ExperimentCard, FilterPill,
'            GradientButton and RoundedPanel controls.
' ============================================================================

experiments ChemLabVirtual

    ' ---------------------------------------------------------------------
    '  Application entry point
    ' ---------------------------------------------------------------------
    Module Program
        <STAThread>
        Sub Main()
            Application.SetHighDpiMode(HighDpiMode.SystemAware)
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.Run(New MainForm())
        End Sub
    End Module

    ' ---------------------------------------------------------------------
    '  Main form: header, search bar, filter pills, experiment card grid
    ' ---------------------------------------------------------------------
    Public Class MainForm
    Inherits Form

    Private ReadOnly colBg As Color = Color.FromArgb(255, 10, 11, 22)
    Private ReadOnly colPanelBg As Color = Color.FromArgb(255, 16, 18, 34)
    Private ReadOnly colBorder As Color = Color.FromArgb(255, 40, 43, 74)
    Private ReadOnly colTextMuted As Color = Color.FromArgb(255, 158, 163, 191)

    Private cardsFlow As FlowLayoutPanel
    Private searchBox As TextBox
    Private pills As New List(Of FilterPill)

    Public Sub New()
        Me.Text = "ChemLab Virtual — Experiments"
        Me.Size = New Size(1280, 860)
        Me.MinimumSize = New Size(760, 560)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = colBg
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.DoubleBuffered = True

        BuildHeader()
        BuildSearchAndFilters()
        BuildCardGrid()
        PopulateSampleCards()
    End Sub

    ' ---------------- Header ----------------
    Private Sub BuildHeader()
        Dim header As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 90,
            .BackColor = colBg,
            .Padding = New Padding(32, 24, 32, 0)
        }

        Dim titleLbl As New Label With {
            .Text = "Experiments",
            .Font = New Font("Segoe UI", 20F, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(32, 14),
            .BackColor = Color.Transparent
        }
        Dim subLbl As New Label With {
            .Text = "Browse the catalogue and launch a guided 3D simulation.",
            .Font = New Font("Segoe UI", 10F),
            .ForeColor = colTextMuted,
            .AutoSize = True,
            .Location = New Point(32, 50),
            .BackColor = Color.Transparent
        }

        Dim newExpBtn As New GradientButton With {
            .Text2 = "New Custom Experiment",
            .Width = 220,
            .Height = 42
        }
        AddHandler Me.Resize, Sub()
                                   newExpBtn.Location = New Point(Me.ClientSize.Width - newExpBtn.Width - 32, 22)
                               End Sub
        newExpBtn.Location = New Point(Me.ClientSize.Width - newExpBtn.Width - 32, 22)

        header.Controls.Add(titleLbl)
        header.Controls.Add(subLbl)
        header.Controls.Add(newExpBtn)
        Me.Controls.Add(header)
    End Sub

    ' ---------------- Search + filter pills ----------------
    Private Sub BuildSearchAndFilters()
        Dim bar As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 110,
            .BackColor = colBg,
            .Padding = New Padding(32, 6, 32, 0)
        }

        ' Search box drawn as a rounded panel containing a borderless TextBox
        Dim searchWrap As New RoundedPanel With {
            .Size = New Size(Me.ClientSize.Width - 64, 42),
            .Location = New Point(32, 6),
            .Anchor = CType(AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right, AnchorStyles),
            .FillColor = colPanelBg,
            .BorderColor = colBorder,
            .Radius = 12
        }
        searchBox = New TextBox With {
            .BorderStyle = BorderStyle.None,
            .BackColor = colPanelBg,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10F),
            .Location = New Point(38, 11),
            .Width = searchWrap.Width - 50
        }
        Dim placeholderShown As Boolean = True
        searchBox.Text = "Search experiments..."
        searchBox.ForeColor = colTextMuted
        AddHandler searchBox.GotFocus, Sub()
                                            If placeholderShown Then
                                                searchBox.Text = ""
                                                searchBox.ForeColor = Color.White
                                                placeholderShown = False
                                            End If
                                        End Sub
        AddHandler searchBox.LostFocus, Sub()
                                             If searchBox.Text.Trim().Length = 0 Then
                                                 searchBox.Text = "Search experiments..."
                                                 searchBox.ForeColor = colTextMuted
                                                 placeholderShown = True
                                             End If
                                         End Sub
        AddHandler searchBox.TextChanged, Sub()
                                               If Not placeholderShown Then FilterCards()
                                           End Sub
        searchWrap.Controls.Add(searchBox)
        AddHandler Me.Resize, Sub()
                                   searchWrap.Width = Me.ClientSize.Width - 64
                                   searchBox.Width = searchWrap.Width - 50
                               End Sub

        ' Filter pill row
        Dim pillFlow As New FlowLayoutPanel With {
            .Location = New Point(32, 58),
            .Size = New Size(Me.ClientSize.Width - 64, 44),
            .Anchor = CType(AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right, AnchorStyles),
            .BackColor = Color.Transparent,
            .WrapContents = False,
            .AutoScroll = False
        }

        Dim names() As String = {"All", "Acids & Bases", "Reactions", "Solutions", "Electrochemistry", "Analysis"}
        For Each n In names
            Dim p As New FilterPill With {.Text2 = n, .Selected = (n = "All")}
            p.AutoSize2()
            p.Margin = New Padding(0, 0, 10, 0)
            AddHandler p.PillClicked, AddressOf OnPillClicked
            pills.Add(p)
            pillFlow.Controls.Add(p)
        Next

        Dim filtersBtn As New FilterPill With {.Text2 = "≡  Filters", .Selected = False}
        filtersBtn.AutoSize2()
        pillFlow.Controls.Add(filtersBtn)

        bar.Controls.Add(pillFlow)
        bar.Controls.Add(searchWrap)
        Me.Controls.Add(bar)
        bar.BringToFront()
    End Sub

    Private Sub OnPillClicked(sender As Object, e As EventArgs)
        For Each p In pills
            p.Selected = (p Is sender)
            p.Invalidate()
        Next
        FilterCards()
    End Sub

    Private Sub FilterCards()
        Dim activeTag As String = "All"
        For Each p In pills
            If p.Selected Then activeTag = p.Text2
        Next
        Dim query As String = searchBox.Text.Trim().ToLowerInvariant()
        If query = "search experiments..." Then query = ""

        For Each ctrl As Control In cardsFlow.Controls
            Dim c = TryCast(ctrl, ExperimentCard)
            If c Is Nothing Then Continue For
            Dim matchesTag As Boolean = (activeTag = "All") OrElse (c.CategoryTag = activeTag)
            Dim matchesQuery As Boolean = (query = "") OrElse c.Title.ToLowerInvariant().Contains(query)
            c.Visible = matchesTag AndAlso matchesQuery
        Next
    End Sub

    ' ---------------- Card grid ----------------
    Private Sub BuildCardGrid()
        cardsFlow = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .BackColor = colBg,
            .AutoScroll = True,
            .Padding = New Padding(32, 10, 24, 24),
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True
        }
        Me.Controls.Add(cardsFlow)
        cardsFlow.BringToFront()
    End Sub

    Private Sub AddCard(title As String, subtitle As String, category As String, level As String,
                         timeText As String, progress As Integer, highlighted As Boolean)
        Dim c As New ExperimentCard With {
            .Title = title,
            .Subtitle = subtitle,
            .CategoryTag = category,
            .LevelTag = level,
            .TimeText = timeText,
            .ProgressPercent = progress,
            .ButtonText = If(progress > 0 AndAlso progress < 100, "Resume", "Start"),
            .Highlighted = highlighted,
            .Margin = New Padding(0, 0, 20, 20),
            .Size = New Size(300, 216)
        }
        AddHandler c.ActionClicked, Sub()
                                         MessageBox.Show($"Launching '{title}'…", "ChemLab Virtual",
                                                          MessageBoxButtons.OK, MessageBoxIcon.Information)
                                     End Sub
        cardsFlow.Controls.Add(c)
    End Sub

    Private Sub PopulateSampleCards()
        AddCard("Acid & Base Reaction", "Observe neutralization reaction",
                "Acids & Bases", "Beginner", "12 min", 100, False)
        AddCard("Precipitation Reaction", "Formation of insoluble precipitate",
                "Reactions", "Intermediate", "15 min", 60, False)
        AddCard("Gas Evolution", "Reaction producing a gas",
                "Reactions", "Beginner", "10 min", 25, False)
        AddCard("Titration", "Find concentration using titration",
                "Solutions", "Advanced", "22 min", 0, True)
        AddCard("Electrolysis of Water", "Split water into hydrogen and oxygen",
                "Electrochemistry", "Intermediate", "18 min", 0, False)
        AddCard("Flame Test", "Identify metal ions by flame colour",
                "Analysis", "Beginner", "8 min", 40, False)
    End Sub

    End Class

    ''' <summary>Simple rounded-corner container panel used for the search bar.</summary>
    Public Class RoundedPanel
    Inherits Panel

    Public Property Radius As Integer = 10
    Public Property FillColor As Color = Color.FromArgb(255, 16, 18, 34)
    Public Property BorderColor As Color = Color.FromArgb(255, 40, 43, 74)

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint Or
                    ControlStyles.ResizeRedraw Or
                    ControlStyles.OptimizedDoubleBuffer, True)
        Me.DoubleBuffered = True
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)
        Dim path As New GraphicsPath()
        Dim d As Integer = Radius * 2
        path.AddArc(rect.X, rect.Y, d, d, 180, 90)
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Using bg As New SolidBrush(FillColor)
            g.FillPath(bg, path)
        End Using
        Using pen As New Pen(BorderColor)
            g.DrawPath(pen, path)
        End Using
        MyBase.OnPaint(e)
    End Sub

    End Class

    ' ---------------------------------------------------------------------
    '  Experiment card control
    ' ---------------------------------------------------------------------
    ''' <summary>
    ''' A single experiment card exactly matching the ChemLab Virtual mock-up:
    ''' gradient flask icon, bookmark, title/subtitle, category+level+time pills,
    ''' progress bar, gradient Start/Resume button and a circular star button.
    ''' </summary>
    Public Class ExperimentCard
    Inherits Panel

    ' ---- Public data / appearance properties ----
    Public Property Title As String = "Experiment"
    Public Property Subtitle As String = "Description"
    Public Property CategoryTag As String = "Reactions"
    Public Property LevelTag As String = "Beginner"
    Public Property TimeText As String = "10 min"
    Public Property ProgressPercent As Integer = 0
    Public Property ButtonText As String = "Start"
    Public Property Highlighted As Boolean = False
    Public Property Bookmarked As Boolean = False
    Public Property Starred As Boolean = False

    ' ---- Colour palette (matches screenshots) ----
    Private ReadOnly colCardBg As Color = Color.FromArgb(255, 22, 24, 45)
    Private ReadOnly colCardBorder As Color = Color.FromArgb(255, 40, 43, 74)
    Private ReadOnly colCardBorderHi As Color = Color.FromArgb(255, 124, 92, 255)
    Private ReadOnly colTitle As Color = Color.White
    Private ReadOnly colSubtitle As Color = Color.FromArgb(255, 158, 163, 191)
    Private ReadOnly colPillBg As Color = Color.FromArgb(255, 32, 35, 61)
    Private ReadOnly colPillBorder As Color = Color.FromArgb(255, 52, 56, 92)
    Private ReadOnly colCategoryText As Color = Color.FromArgb(255, 74, 222, 222)
    Private ReadOnly colTrack As Color = Color.FromArgb(255, 40, 43, 68)
    Private ReadOnly colGradA As Color = Color.FromArgb(255, 124, 92, 255)   ' purple
    Private ReadOnly colGradB As Color = Color.FromArgb(255, 236, 72, 187)   ' pink

    ' ---- Layout regions used for hit-testing ----
    Private btnRect As Rectangle
    Private starRect As Rectangle
    Private bookmarkRect As Rectangle
    Private btnHover As Boolean = False
    Private starHover As Boolean = False

    Public Event ActionClicked(sender As Object, e As EventArgs)
    Public Event StarClicked(sender As Object, e As EventArgs)
    Public Event BookmarkClicked(sender As Object, e As EventArgs)

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint Or
                    ControlStyles.ResizeRedraw Or
                    ControlStyles.OptimizedDoubleBuffer Or
                    ControlStyles.SupportsTransparentBackColor, True)
        Me.DoubleBuffered = True
        Me.Size = New Size(300, 210)
        Me.BackColor = Color.FromArgb(255, 10, 11, 22)
        Me.Cursor = Cursors.Default
        Me.Font = New Font("Segoe UI", 9.5F)
    End Sub

    ' ---------- Helpers ----------

    Private Function RoundedRect(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim p As New GraphicsPath()
        Dim d As Integer = radius * 2
        p.StartFigure()
        p.AddArc(rect.X, rect.Y, d, d, 180, 90)
        p.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        p.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        p.CloseFigure()
        Return p
    End Function

    Private Function BrandGradient(rect As Rectangle) As LinearGradientBrush
        Return New LinearGradientBrush(rect, colGradA, colGradB, LinearGradientMode.Horizontal)
    End Function

    Private Sub DrawPill(g As Graphics, ByRef x As Integer, y As Integer, text As String,
                          fg As Color, useCategoryStyle As Boolean, clockIcon As Boolean)
        Using f As New Font("Segoe UI", 8.5F, FontStyle.Regular)
            Dim extra As Integer = If(clockIcon, 16, 0)
            Dim sz As SizeF = g.MeasureString(text, f)
            Dim w As Integer = CInt(sz.Width) + 20 + extra
            Dim h As Integer = 24
            Dim r As New Rectangle(x, y, w, h)
            Using path As GraphicsPath = RoundedRect(r, h \ 2)
                Using bg As New SolidBrush(colPillBg)
                    g.FillPath(bg, path)
                End Using
                Using pen As New Pen(If(useCategoryStyle, Color.FromArgb(255, 45, 90, 90), colPillBorder))
                    g.DrawPath(pen, path)
                End Using
            End Using
            Dim tx As Integer = x + 10
            If clockIcon Then
                DrawClockIcon(g, x + 8, y + h \ 2 - 6, fg)
                tx = x + 22
            End If
            Using tb As New SolidBrush(fg)
                g.DrawString(text, f, tb, tx, y + (h - sz.Height) / 2.0F)
            End Using
            x += w + 8
        End Using
    End Sub

    Private Sub DrawClockIcon(g As Graphics, x As Integer, y As Integer, c As Color)
        Using pen As New Pen(c, 1.4F)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.DrawEllipse(pen, x, y, 12, 12)
            g.DrawLine(pen, x + 6, y + 3, x + 6, y + 6)
            g.DrawLine(pen, x + 6, y + 6, x + 9, y + 8)
        End Using
    End Sub

    Private Sub DrawFlaskIcon(g As Graphics, rect As Rectangle)
        ' circular gradient badge
        Using gb As New LinearGradientBrush(rect, colGradA, colGradB, LinearGradientMode.ForwardDiagonal)
            g.FillEllipse(gb, rect)
        End Using
        ' simple flask glyph in white
        Dim cx As Single = rect.X + rect.Width / 2.0F
        Dim cy As Single = rect.Y + rect.Height / 2.0F
        Using pen As New Pen(Color.White, 1.6F)
            g.SmoothingMode = SmoothingMode.AntiAlias
            ' neck
            g.DrawLine(pen, cx - 3, cy - 9, cx - 3, cy - 3)
            g.DrawLine(pen, cx + 3, cy - 9, cx + 3, cy - 3)
            g.DrawLine(pen, cx - 5, cy - 9, cx + 5, cy - 9)
            ' flask body (triangle-ish)
            Dim pts() As PointF = {
                New PointF(cx - 3, cy - 3),
                New PointF(cx - 8, cy + 8),
                New PointF(cx + 8, cy + 8),
                New PointF(cx + 3, cy - 3)
            }
            g.DrawLines(pen, pts)
            g.DrawLine(pen, pts(1), pts(2))
        End Using
        ' liquid fill
        Using liquidBrush As New SolidBrush(Color.FromArgb(160, 255, 255, 255))
            Dim liquid() As PointF = {
                New PointF(cx - 6, cy + 3),
                New PointF(cx - 8, cy + 8),
                New PointF(cx + 8, cy + 8),
                New PointF(cx + 6, cy + 3)
            }
            g.FillPolygon(liquidBrush, liquid)
        End Using
    End Sub

    Private Sub DrawBookmark(g As Graphics, rect As Rectangle, filled As Boolean)
        Dim pts() As PointF = {
            New PointF(rect.X, rect.Y),
            New PointF(rect.Right, rect.Y),
            New PointF(rect.Right, rect.Bottom),
            New PointF(rect.X + rect.Width / 2.0F, rect.Bottom - 6),
            New PointF(rect.X, rect.Bottom)
        }
        g.SmoothingMode = SmoothingMode.AntiAlias
        If filled Then
            Using b As New SolidBrush(Color.FromArgb(255, 200, 205, 225))
                g.FillPolygon(b, pts)
            End Using
        Else
            Using pen As New Pen(Color.FromArgb(255, 120, 125, 155), 1.4F)
                g.DrawPolygon(pen, pts)
            End Using
        End If
    End Sub

    Private Sub DrawStar(g As Graphics, rect As Rectangle, filled As Boolean)
        Dim cx As Single = rect.X + rect.Width / 2.0F
        Dim cy As Single = rect.Y + rect.Height / 2.0F
        Dim outerR As Single = rect.Width / 2.5F
        Dim innerR As Single = outerR * 0.45F
        Dim pts(9) As PointF
        For i As Integer = 0 To 9
            Dim ang As Double = Math.PI / 2 * 3 + i * Math.PI / 5
            Dim r As Single = If(i Mod 2 = 0, outerR, innerR)
            pts(i) = New PointF(cx + CSng(Math.Cos(ang) * r), cy + CSng(Math.Sin(ang) * r))
        Next
        g.SmoothingMode = SmoothingMode.AntiAlias
        If filled Then
            Using b As New SolidBrush(Color.FromArgb(255, 236, 72, 187))
                g.FillPolygon(b, pts)
            End Using
        Else
            Using pen As New Pen(Color.FromArgb(255, 150, 155, 180), 1.3F)
                g.DrawPolygon(pen, pts)
            End Using
        End If
    End Sub

    ' ---------- Painting ----------

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit

        Dim full As New Rectangle(1, 1, Me.Width - 3, Me.Height - 3)
        Using path As GraphicsPath = RoundedRect(full, 14)
            Using bg As New SolidBrush(colCardBg)
                g.FillPath(bg, path)
            End Using
            Using pen As New Pen(If(Highlighted, colCardBorderHi, colCardBorder), If(Highlighted, 1.6F, 1.0F))
                g.DrawPath(pen, path)
            End Using
        End Using

        Dim pad As Integer = 18

        ' Icon badge
        Dim iconRect As New Rectangle(pad, pad, 40, 40)
        DrawFlaskIcon(g, iconRect)

        ' Bookmark (top right)
        bookmarkRect = New Rectangle(Me.Width - pad - 16, pad + 2, 14, 16)
        DrawBookmark(g, bookmarkRect, Bookmarked)

        ' Title
        Dim titleY As Integer = pad
        Using tf As New Font("Segoe UI", 11.5F, FontStyle.Bold)
            Using tb As New SolidBrush(colTitle)
                g.DrawString(Title, tf, tb, iconRect.Right + 12, titleY - 1)
            End Using
        End Using

        ' Subtitle
        Using sf As New Font("Segoe UI", 9F)
            Using sb As New SolidBrush(colSubtitle)
                g.DrawString(Subtitle, sf, sb, iconRect.Right + 12, titleY + 22)
            End Using
        End Using

        ' Pills row
        Dim px As Integer = pad
        Dim py As Integer = pad + 54
        DrawPill(g, px, py, CategoryTag, colCategoryText, True, False)
        DrawPill(g, px, py, LevelTag, Color.FromArgb(255, 190, 194, 214), False, False)
        DrawPill(g, px, py, TimeText, Color.FromArgb(255, 190, 194, 214), False, True)

        ' Progress label row
        Dim progY As Integer = py + 40
        Using lf As New Font("Segoe UI", 8.5F)
            Using lb As New SolidBrush(colSubtitle)
                g.DrawString("Progress", lf, lb, pad, progY)
            End Using
            Dim pct As String = ProgressPercent & "%"
            Dim pctSz As SizeF = g.MeasureString(pct, lf)
            Using pb As New SolidBrush(colTitle)
                g.DrawString(pct, lf, pb, Me.Width - pad - pctSz.Width, progY)
            End Using
        End Using

        ' Progress track + fill
        Dim trackRect As New Rectangle(pad, progY + 20, Me.Width - pad * 2, 6)
        Using tp As GraphicsPath = RoundedRect(trackRect, 3)
            Using tb As New SolidBrush(colTrack)
                g.FillPath(tb, tp)
            End Using
        End Using
        If ProgressPercent > 0 Then
            Dim fillW As Integer = CInt(trackRect.Width * (Math.Min(ProgressPercent, 100) / 100.0))
            fillW = Math.Max(fillW, 8)
            Dim fillRect As New Rectangle(trackRect.X, trackRect.Y, fillW, trackRect.Height)
            Using fp As GraphicsPath = RoundedRect(fillRect, 3)
                Using fb As LinearGradientBrush = BrandGradient(New Rectangle(trackRect.X, trackRect.Y, Math.Max(trackRect.Width, 1), trackRect.Height))
                    g.FillPath(fb, fp)
                End Using
            End Using
        End If

        ' Button + star row
        Dim rowY As Integer = trackRect.Bottom + 16
        Dim starSize As Integer = 40
        starRect = New Rectangle(Me.Width - pad - starSize, rowY, starSize, starSize)
        btnRect = New Rectangle(pad, rowY, starRect.X - pad - 10, starSize)

        Using bp As GraphicsPath = RoundedRect(btnRect, starSize \ 2)
            Using bb As LinearGradientBrush = BrandGradient(btnRect)
                If btnHover Then
                    Using overlay As New SolidBrush(Color.FromArgb(30, 255, 255, 255))
                        g.FillPath(bb, bp)
                        g.FillPath(overlay, bp)
                    End Using
                Else
                    g.FillPath(bb, bp)
                End If
            End Using
            Using bf As New Font("Segoe UI", 10F, FontStyle.Bold)
                Dim tsz As SizeF = g.MeasureString(ButtonText, bf)
                Using tb As New SolidBrush(Color.White)
                    g.DrawString(ButtonText, bf, tb,
                                 btnRect.X + (btnRect.Width - tsz.Width) / 2.0F,
                                 btnRect.Y + (btnRect.Height - tsz.Height) / 2.0F)
                End Using
            End Using
        End Using

        Using sp As GraphicsPath = RoundedRect(starRect, starSize \ 2)
            Using sBg As New SolidBrush(If(starHover, Color.FromArgb(255, 34, 37, 62), colPillBg))
                g.FillPath(sBg, sp)
            End Using
            Using sPen As New Pen(colPillBorder)
                g.DrawPath(sPen, sp)
            End Using
        End Using
        Dim starIconRect As New Rectangle(starRect.X + 10, starRect.Y + 10, 20, 20)
        DrawStar(g, starIconRect, Starred)

        MyBase.OnPaint(e)
    End Sub

    ' ---------- Mouse interaction ----------

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        Dim wasBtn = btnHover
        Dim wasStar = starHover
        btnHover = btnRect.Contains(e.Location)
        starHover = starRect.Contains(e.Location)
        Me.Cursor = If(btnHover OrElse starHover OrElse bookmarkRect.Contains(e.Location), Cursors.Hand, Cursors.Default)
        If wasBtn <> btnHover OrElse wasStar <> starHover Then Me.Invalidate()
        MyBase.OnMouseMove(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        btnHover = False
        starHover = False
        Me.Invalidate()
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        If btnRect.Contains(e.Location) Then
            RaiseEvent ActionClicked(Me, EventArgs.Empty)
        ElseIf starRect.Contains(e.Location) Then
            Starred = Not Starred
            Me.Invalidate()
            RaiseEvent StarClicked(Me, EventArgs.Empty)
        ElseIf bookmarkRect.Contains(e.Location) Then
            Bookmarked = Not Bookmarked
            Me.Invalidate()
            RaiseEvent BookmarkClicked(Me, EventArgs.Empty)
        End If
        MyBase.OnMouseClick(e)
    End Sub

    End Class

    ' ---------------------------------------------------------------------
    '  Filter pill toggle control
    ' ---------------------------------------------------------------------
    ''' <summary>Rounded pill-shaped toggle button used for the category filter row.</summary>
    Public Class FilterPill
    Inherits Panel

    Public Property Text2 As String = "All"
    Public Property Selected As Boolean = False

    Private ReadOnly colGradA As Color = Color.FromArgb(255, 124, 92, 255)
    Private ReadOnly colGradB As Color = Color.FromArgb(255, 236, 72, 187)
    Private ReadOnly colUnselectedBg As Color = Color.FromArgb(255, 22, 24, 45)
    Private ReadOnly colUnselectedBorder As Color = Color.FromArgb(255, 46, 50, 82)
    Private ReadOnly colUnselectedText As Color = Color.FromArgb(255, 190, 194, 214)

    Public Event PillClicked(sender As Object, e As EventArgs)

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint Or
                    ControlStyles.ResizeRedraw Or
                    ControlStyles.OptimizedDoubleBuffer, True)
        Me.DoubleBuffered = True
        Me.Cursor = Cursors.Hand
        Me.Height = 34
        Me.Font = New Font("Segoe UI", 9.5F)
    End Sub

    Public Sub AutoSize2()
        Using g As Graphics = Me.CreateGraphics()
            Dim sz As SizeF = g.MeasureString(Text2, Me.Font)
            Me.Width = CInt(sz.Width) + 32
        End Using
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)
        Dim path As New GraphicsPath()
        Dim d As Integer = rect.Height
        path.AddArc(rect.X, rect.Y, d, d, 90, 180)
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 180)
        path.CloseFigure()

        If Selected Then
            Using bg As New LinearGradientBrush(rect, colGradA, colGradB, LinearGradientMode.Horizontal)
                g.FillPath(bg, path)
            End Using
        Else
            Using bg As New SolidBrush(colUnselectedBg)
                g.FillPath(bg, path)
            End Using
            Using pen As New Pen(colUnselectedBorder)
                g.DrawPath(pen, path)
            End Using
        End If

        Dim fg As Color = If(Selected, Color.White, colUnselectedText)
        Using tb As New SolidBrush(fg)
            Dim sz As SizeF = g.MeasureString(Text2, Me.Font)
            g.DrawString(Text2, Me.Font, tb, (Me.Width - sz.Width) / 2.0F, (Me.Height - sz.Height) / 2.0F)
        End Using
    End Sub

    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        RaiseEvent PillClicked(Me, EventArgs.Empty)
        MyBase.OnMouseClick(e)
    End Sub

    End Class

    ' ---------------------------------------------------------------------
    '  Gradient rounded button control
    ' ---------------------------------------------------------------------
    Public Class GradientButton
    Inherits Panel

    Public Property Text2 As String = "Button"
    Private ReadOnly colGradA As Color = Color.FromArgb(255, 124, 92, 255)
    Private ReadOnly colGradB As Color = Color.FromArgb(255, 236, 72, 187)
    Private hover As Boolean = False

    Public Event ButtonClicked(sender As Object, e As EventArgs)

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint Or
                    ControlStyles.ResizeRedraw Or
                    ControlStyles.OptimizedDoubleBuffer, True)
        Me.DoubleBuffered = True
        Me.Cursor = Cursors.Hand
        Me.Height = 42
        Me.Width = 190
        Me.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)
        Dim path As New GraphicsPath()
        Dim d As Integer = rect.Height
        path.AddArc(rect.X, rect.Y, d, d, 90, 180)
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 180)
        path.CloseFigure()
        Using bg As New LinearGradientBrush(rect, colGradA, colGradB, LinearGradientMode.Horizontal)
            g.FillPath(bg, path)
        End Using
        If hover Then
            Using overlay As New SolidBrush(Color.FromArgb(28, 255, 255, 255))
                g.FillPath(overlay, path)
            End Using
        End If
        Using tb As New SolidBrush(Color.White)
            Dim sz As SizeF = g.MeasureString(Text2, Me.Font)
            g.DrawString(Text2, Me.Font, tb, (Me.Width - sz.Width) / 2.0F, (Me.Height - sz.Height) / 2.0F)
        End Using
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        hover = True : Me.Invalidate() : MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        hover = False : Me.Invalidate() : MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        RaiseEvent ButtonClicked(Me, EventArgs.Empty)
        MyBase.OnMouseClick(e)
    End Sub

    End Class

End experiments