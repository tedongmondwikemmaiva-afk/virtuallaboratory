Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Public Class SplashForm
    Inherits Form

    Private WithEvents progressTimer As New Timer()
    Private currentProgress As Integer = 0
    Private spinnerAngle As Single = 0
    Private lblSkip As LinkLabel

    Public Sub New()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(1000, 620)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.DoubleBuffered = True
        Me.BackColor = Color.FromArgb(8, 11, 26)
        Me.Text = "ChemLab Virtual"

        InitializeControls()

        progressTimer.Interval = 60
        progressTimer.Start()
    End Sub

    Private Sub InitializeControls()
        lblSkip = New LinkLabel()
        lblSkip.Text = "Skip intro"
        lblSkip.AutoSize = True
        lblSkip.Font = New Font("Segoe UI", 11, FontStyle.Regular)
        lblSkip.LinkColor = Color.FromArgb(56, 214, 255)
        lblSkip.ActiveLinkColor = Color.FromArgb(90, 225, 255)
        lblSkip.VisitedLinkColor = Color.FromArgb(56, 214, 255)
        lblSkip.LinkBehavior = LinkBehavior.HoverUnderline
        lblSkip.BackColor = Color.Transparent
        lblSkip.Cursor = Cursors.Hand
        Me.Controls.Add(lblSkip)
        AddHandler lblSkip.LinkClicked, AddressOf SkipIntro_Click
        AddHandler Me.Resize, AddressOf SplashForm_Resize

        PositionControls()
    End Sub

    Private Sub SplashForm_Resize(sender As Object, e As EventArgs)
        PositionControls()
        Me.Invalidate()
    End Sub

    Private Sub PositionControls()
        lblSkip.Location = New Point((Me.Width - lblSkip.Width) \ 2, 460)
    End Sub

    Private Sub SkipIntro_Click(sender As Object, e As EventArgs)
        ' Close the splash and open your main form here, e.g.:
        ' Dim main As New MainForm()
        ' main.Show()
        Me.Close()
    End Sub

    Private Sub ProgressTimer_Tick(sender As Object, e As EventArgs) Handles progressTimer.Tick
        currentProgress += 1
        spinnerAngle += 24
        If spinnerAngle >= 360 Then spinnerAngle -= 360

        If currentProgress >= 100 Then
            currentProgress = 100
            progressTimer.Stop()
            Me.Close() ' hands off to LoginForm, wired in Program.vb
        End If

        Me.Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit

        DrawBackground(g)
        DrawFlaskIcon(g)
        DrawTitles(g)
        DrawLoadingText(g)
        DrawProgressBar(g)
        DrawSpinnerText(g)
        DrawFooter(g)
    End Sub

    ' ---------- Background ----------

    Private Sub DrawBackground(g As Graphics)
        Using brush As New LinearGradientBrush(Me.ClientRectangle, Color.FromArgb(12, 15, 32), Color.FromArgb(6, 8, 18), 90.0F)
            g.FillRectangle(brush, Me.ClientRectangle)
        End Using

        ' faint decorative "molecule" dots, top-right, like the reference image
        Dim pts As New List(Of PointF) From {
            New PointF(Me.Width - 260, 60),
            New PointF(Me.Width - 180, 95),
            New PointF(Me.Width - 300, 130),
            New PointF(Me.Width - 210, 170)
        }
        Using linePen As New Pen(Color.FromArgb(18, 255, 255, 255), 1.5F)
            For i As Integer = 0 To pts.Count - 2
                g.DrawLine(linePen, pts(i), pts(i + 1))
            Next
        End Using
        Using dotBrush As New SolidBrush(Color.FromArgb(28, 255, 255, 255))
            For Each p In pts
                g.FillEllipse(dotBrush, p.X - 5, p.Y - 5, 10, 10)
            Next
        End Using
    End Sub

    ' ---------- Icon ----------

    Private Sub DrawFlaskIcon(g As Graphics)
        Dim boxSize As Integer = 90
        Dim boxX As Integer = (Me.Width \ 2) - 160
        Dim boxY As Integer = 155

        Dim rect As New Rectangle(boxX, boxY, boxSize, boxSize)
        Using path As GraphicsPath = RoundedRect(rect, 20)
            Using gradBrush As New LinearGradientBrush(rect, Color.FromArgb(255, 108, 92, 231), Color.FromArgb(255, 214, 82, 205), 45.0F)
                g.FillPath(gradBrush, path)
            End Using
        End Using

        Dim cx As Single = boxX + boxSize / 2.0F
        Dim cy As Single = boxY + boxSize / 2.0F

        Using flaskPen As New Pen(Color.White, 3)
            flaskPen.LineJoin = LineJoin.Round
            flaskPen.StartCap = LineCap.Round
            flaskPen.EndCap = LineCap.Round

            Dim neckTopL As New PointF(cx - 6, cy - 24)
            Dim neckTopR As New PointF(cx + 6, cy - 24)
            Dim neckBotL As New PointF(cx - 6, cy - 4)
            Dim neckBotR As New PointF(cx + 6, cy - 4)
            Dim bodyL As New PointF(cx - 22, cy + 22)
            Dim bodyR As New PointF(cx + 22, cy + 22)

            g.DrawLine(flaskPen, New PointF(neckTopL.X - 5, neckTopL.Y), New PointF(neckTopR.X + 5, neckTopR.Y)) ' mouth
            g.DrawLine(flaskPen, neckTopL, neckBotL)
            g.DrawLine(flaskPen, neckTopR, neckBotR)
            g.DrawLine(flaskPen, neckBotL, bodyL)
            g.DrawLine(flaskPen, neckBotR, bodyR)

            Dim basePts() As PointF = {bodyL, New PointF(bodyL.X + 4, bodyR.Y + 6), New PointF(bodyR.X - 4, bodyR.Y + 6), bodyR}
            g.DrawLines(flaskPen, basePts)
        End Using
    End Sub

    Private Function RoundedRect(bounds As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d As Integer = radius * 2
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90)
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90)
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90)
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    ' ---------- Text ----------

    Private Sub DrawTitles(g As Graphics)
        Using titleFont As New Font("Segoe UI", 28, FontStyle.Bold)
            Using subFont As New Font("Segoe UI", 10, FontStyle.Regular)
                Dim titleX As Integer = (Me.Width \ 2) - 55
                Dim titleY As Integer = 158

                TextRenderer.DrawText(g, "ChemLab", titleFont, New Point(titleX, titleY), Color.White, TextFormatFlags.NoPadding)

                Using vBrush As New SolidBrush(Color.FromArgb(160, 170, 200))
                    g.DrawString("V I R T U A L", subFont, vBrush, titleX + 2, titleY + 44)
                End Using
            End Using
        End Using
    End Sub

    Private Sub DrawLoadingText(g As Graphics)
        Using f As New Font("Segoe UI", 13, FontStyle.Regular)
            Dim text As String = "Loading 3D apparatus, shaders and reaction engine…"
            Using brush As New SolidBrush(Color.FromArgb(190, 197, 214))
                Dim size As SizeF = g.MeasureString(text, f)
                g.DrawString(text, f, brush, (Me.Width - size.Width) / 2.0F, 295)
            End Using
        End Using
    End Sub

    ' ---------- Progress bar ----------

    Private Sub DrawProgressBar(g As Graphics)
        Dim barWidth As Integer = 580
        Dim barHeight As Integer = 6
        Dim barX As Integer = (Me.Width - barWidth) \ 2
        Dim barY As Integer = 350

        Dim trackRect As New Rectangle(barX, barY, barWidth, barHeight)
        Using trackPath As GraphicsPath = RoundedRect(trackRect, barHeight \ 2)
            Using trackBrush As New SolidBrush(Color.FromArgb(40, 44, 66))
                g.FillPath(trackBrush, trackPath)
            End Using
        End Using

        Dim fillWidth As Integer = CInt(barWidth * (currentProgress / 100.0F))
        If fillWidth > 0 Then
            Dim fillRect As New Rectangle(barX, barY, Math.Max(fillWidth, barHeight), barHeight)
            Using fillPath As GraphicsPath = RoundedRect(fillRect, barHeight \ 2)
                Using fillBrush As New LinearGradientBrush(New Rectangle(barX, barY, barWidth, barHeight), Color.FromArgb(108, 92, 231), Color.FromArgb(214, 82, 205), 0.0F)
                    g.FillPath(fillBrush, fillPath)
                End Using
            End Using
        End If
    End Sub

    Private Sub DrawSpinnerText(g As Graphics)
        Using f As New Font("Segoe UI", 12, FontStyle.Regular)
            Dim text As String = $"Initialising renderer — {currentProgress}%"
            Dim size As SizeF = g.MeasureString(text, f)
            Dim spinnerSize As Integer = 18
            Dim totalWidth As Single = size.Width + spinnerSize + 10
            Dim startX As Single = (Me.Width - totalWidth) / 2.0F
            Dim y As Integer = 380

            Using pen As New Pen(Color.FromArgb(150, 158, 180), 2)
                Dim state As GraphicsState = g.Save()
                g.TranslateTransform(startX + spinnerSize / 2.0F, y + spinnerSize / 2.0F)
                g.RotateTransform(spinnerAngle)
                g.DrawArc(pen, -spinnerSize / 2.0F, -spinnerSize / 2.0F, spinnerSize, spinnerSize, 0, 270)
                g.Restore(state)
            End Using

            Using brush As New SolidBrush(Color.FromArgb(190, 197, 214))
                g.DrawString(text, f, brush, startX + spinnerSize + 10, y - 2)
            End Using
        End Using
    End Sub

    Private Sub DrawFooter(g As Graphics)
        Using f As New Font("Segoe UI", 9, FontStyle.Regular)
            Dim text As String = "Version 1.0.0 · © 2026 CodegisoftAcademy"
            Using brush As New SolidBrush(Color.FromArgb(110, 118, 140))
                Dim size As SizeF = g.MeasureString(text, f)
                g.DrawString(text, f, brush, (Me.Width - size.Width) / 2.0F, Me.Height - 40)
            End Using
        End Using
    End Sub

End Class
