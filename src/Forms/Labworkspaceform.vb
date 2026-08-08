Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Namespace ChemLabDesign

    ''' <summary>
    ''' Central color / font palette matching the reference UI (dark navy glass theme
    ''' with a purple -> blue accent gradient).
    ''' </summary>
    Public Module Theme
        Public ReadOnly BgTop As Color = Color.FromArgb(255, 10, 13, 24)
        Public ReadOnly BgBottom As Color = Color.FromArgb(255, 16, 20, 36)

        Public ReadOnly CardFill As Color = Color.FromArgb(235, 22, 27, 46)
        Public ReadOnly CardFillLight As Color = Color.FromArgb(235, 26, 32, 54)
        Public ReadOnly CardBorder As Color = Color.FromArgb(40, 255, 255, 255)

        Public ReadOnly AccentPurple As Color = Color.FromArgb(255, 124, 92, 246)
        Public ReadOnly AccentBlue As Color = Color.FromArgb(255, 79, 140, 255)
        Public ReadOnly AccentGreen As Color = Color.FromArgb(255, 46, 204, 133)
        Public ReadOnly AccentTeal As Color = Color.FromArgb(255, 45, 212, 191)

        Public ReadOnly TextPrimary As Color = Color.FromArgb(255, 236, 239, 248)
        Public ReadOnly TextSecondary As Color = Color.FromArgb(255, 148, 158, 184)
        Public ReadOnly TextMuted As Color = Color.FromArgb(255, 100, 110, 138)

        Public ReadOnly FontFamily As String = "Segoe UI"

        Public Function TitleFont(Optional size As Single = 13.0F) As Font
            Return New Font(FontFamily, size, FontStyle.Bold, GraphicsUnit.Point)
        End Function

        Public Function BodyFont(Optional size As Single = 9.5F, Optional bold As Boolean = False) As Font
            Return New Font(FontFamily, size, If(bold, FontStyle.Bold, FontStyle.Regular), GraphicsUnit.Point)
        End Function

        Public Function RoundedRect(rect As Rectangle, radius As Integer) As GraphicsPath
            Dim d As Integer = radius * 2
            Dim path As New GraphicsPath()
            If d > rect.Width Then d = rect.Width
            If d > rect.Height Then d = rect.Height

            path.StartFigure()
            path.AddArc(rect.X, rect.Y, d, d, 180, 90)
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            Return path
        End Function
    End Module

    ''' <summary>
    ''' A "glass" rounded-corner card, the base building block of every panel in the UI
    ''' (Apparatus, Instructions, Steps, Results, etc).
    ''' </summary>
    Public Class GlassCard
        Inherits Panel

        Public Property CornerRadius As Integer = 18
        Public Property FillColor As Color = Theme.CardFill
        Public Property BorderColor As Color = Theme.CardBorder
        Public Property BorderWidth As Single = 1.2F
        Public Property HighlightBorder As Boolean = False
        Public Property HighlightColor As Color = Theme.AccentPurple

        Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            Me.BackColor = Color.Transparent
            Me.Padding = New Padding(18)
        End Sub

        Protected Overrides Sub OnPaintBackground(pevent As PaintEventArgs)
            ' Intentionally do nothing – avoids flat rectangle painting under the rounded path.
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)
            Using path As GraphicsPath = Theme.RoundedRect(rect, CornerRadius)
                Using brush As New SolidBrush(FillColor)
                    e.Graphics.FillPath(brush, path)
                End Using
                Dim bColor = If(HighlightBorder, HighlightColor, BorderColor)
                Dim bWidth = If(HighlightBorder, 1.6F, BorderWidth)
                Using pen As New Pen(bColor, bWidth)
                    e.Graphics.DrawPath(pen, path)
                End Using
            End Using
            MyBase.OnPaint(e)
        End Sub
    End Class

    ''' <summary>
    ''' Purple -> blue gradient pill button, used for primary actions
    ''' ("View Theory", "Save to Notebook", nav tabs, etc).
    ''' </summary>
    Public Class GradientButton
        Inherits Button

        Public Property CornerRadius As Integer = 22
        Public Property ColorStart As Color = Theme.AccentPurple
        Public Property ColorEnd As Color = Theme.AccentBlue
        Public Property OutlineOnly As Boolean = False
        Public Property IconText As String = ""

        Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            FlatStyle = FlatStyle.Flat
            FlatAppearance.BorderSize = 0
            FlatAppearance.MouseOverBackColor = Color.Transparent
            FlatAppearance.MouseDownBackColor = Color.Transparent
            ForeColor = Theme.TextPrimary
            Font = Theme.BodyFont(9.75F, True)
            Cursor = Cursors.Hand
            BackColor = Color.Transparent
        End Sub

        Protected Overrides Sub OnPaintBackground(pevent As PaintEventArgs)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            e.Graphics.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
            Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)
            Using path As GraphicsPath = Theme.RoundedRect(rect, CornerRadius)
                If OutlineOnly Then
                    Using brush As New SolidBrush(Color.FromArgb(30, 255, 255, 255))
                        e.Graphics.FillPath(brush, path)
                    End Using
                    Using pen As New Pen(Color.FromArgb(70, 255, 255, 255), 1.2F)
                        e.Graphics.DrawPath(pen, path)
                    End Using
                Else
                    Using brush As New LinearGradientBrush(rect, ColorStart, ColorEnd, LinearGradientMode.Horizontal)
                        e.Graphics.FillPath(brush, path)
                    End Using
                End If
            End Using

            Dim text = Me.Text
            If IconText <> "" Then text = IconText & "  " & text
            TextRenderer.DrawText(e.Graphics, text, Font, rect, ForeColor,
                                   TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
        End Sub
    End Class

    ''' <summary>
    ''' Small segmented pill toggle used for "3D / Top / Side / Front" and "Steps / Notes".
    ''' </summary>
    Public Class PillToggle
        Inherits Panel

        Private _buttons As New List(Of GradientButton)
        Public Event SelectionChanged(sender As Object, index As Integer)
        Private _selectedIndex As Integer = 0

        Public ReadOnly Property SelectedIndex As Integer
            Get
                Return _selectedIndex
            End Get
        End Property

        Sub New(options As String())
            Me.BackColor = Color.Transparent
            Dim x As Integer = 0
            For i As Integer = 0 To options.Length - 1
                Dim idx = i
                Dim btn As New GradientButton() With {
                    .Text = options(i),
                    .Width = TextRenderer.MeasureText(options(i), Theme.BodyFont(9.25F, True)).Width + 28,
                    .Height = 30,
                    .Left = x,
                    .Top = 0,
                    .CornerRadius = 14,
                    .OutlineOnly = (i <> 0)
                }
                If i = 0 Then
                    btn.ColorStart = Theme.AccentPurple
                    btn.ColorEnd = Theme.AccentBlue
                Else
                    btn.ForeColor = Theme.TextSecondary
                End If
                AddHandler btn.Click, Sub(s, e) SelectOption(idx)
                Me.Controls.Add(btn)
                _buttons.Add(btn)
                x += btn.Width + 8
            Next
            Me.Width = x
            Me.Height = 30
        End Sub

        Private Sub SelectOption(index As Integer)
            _selectedIndex = index
            For i As Integer = 0 To _buttons.Count - 1
                Dim b = _buttons(i)
                b.OutlineOnly = (i <> index)
                b.ForeColor = If(i = index, Theme.TextPrimary, Theme.TextSecondary)
                b.Invalidate()
            Next
            RaiseEvent SelectionChanged(Me, index)
        End Sub
    End Class

    ''' <summary>
    ''' Backdrop panel: dark vertical gradient plus a couple of soft blurred "glow" blobs,
    ''' mimicking the ambient lab-background photo behind the glass cards.
    ''' </summary>
    Public Class BackdropPanel
        Inherits Panel

        Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim rect As New Rectangle(0, 0, Width, Height)

            Using bgBrush As New LinearGradientBrush(rect, Theme.BgTop, Theme.BgBottom, LinearGradientMode.Vertical)
                g.FillRectangle(bgBrush, rect)
            End Using

            DrawGlow(g, New Point(CInt(Width * 0.15), CInt(Height * 0.25)), 260, Color.FromArgb(60, 124, 92, 246))
            DrawGlow(g, New Point(CInt(Width * 0.85), CInt(Height * 0.65)), 320, Color.FromArgb(50, 79, 140, 255))
            DrawGlow(g, New Point(CInt(Width * 0.6), CInt(Height * 0.15)), 200, Color.FromArgb(40, 224, 122, 95))

            MyBase.OnPaint(e)
        End Sub

        Private Sub DrawGlow(g As Graphics, center As Point, radius As Integer, c As Color)
            Dim rect As New Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2)
            Using path As New GraphicsPath()
                path.AddEllipse(rect)
                Using brush As New PathGradientBrush(path)
                    brush.CenterColor = c
                    brush.SurroundColors = New Color() {Color.FromArgb(0, c.R, c.G, c.B)}
                    g.FillPath(brush, path)
                End Using
            End Using
        End Sub
    End Class

End Namespace