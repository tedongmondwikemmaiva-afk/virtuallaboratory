Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting

Public Class ReportsGrades
    Inherits Form

    Private headerLabel As Label
    Private subtitleLabel As Label
    Private termCombo As ComboBox
    Private exportButton As Button
    Private cardsFlow As FlowLayoutPanel
    Private chartPanel As Panel
    Private scoreChart As Chart
    Private masteryChart As Chart
    Private historyGrid As DataGridView
    Private leftNavPanel As Panel

    Public Sub New()
        InitializeComponent()
        PopulateSampleData()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Reports & Grades"
        Me.Size = New Size(1100, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(22, 28, 35)
        Me.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point)

        ' Left navigation (simple placeholder)
        leftNavPanel = New Panel() With {
            .BackColor = Color.FromArgb(18, 24, 30),
            .Dock = DockStyle.Left,
            .Width = 180
        }
        Me.Controls.Add(leftNavPanel)

        headerLabel = New Label() With {
            .Text = "Reports & Grades",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 16.0F, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(200, 16)
        }
        Me.Controls.Add(headerLabel)

        subtitleLabel = New Label() With {
            .Text = "Assessment history, topic mastery and downloadable transcripts.",
            .ForeColor = Color.FromArgb(170, 170, 170),
            .Font = New Font("Segoe UI", 9.0F),
            .AutoSize = True,
            .Location = New Point(200, 46)
        }
        Me.Controls.Add(subtitleLabel)

        termCombo = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .BackColor = Color.FromArgb(30, 36, 44),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(Me.ClientSize.Width - 240, 20),
            .Width = 90,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        termCombo.Items.AddRange(New String() {"Term 1", "Term 2", "Term 3"})
        termCombo.SelectedIndex = 0
        Me.Controls.Add(termCombo)

        exportButton = New Button() With {
            .Text = "Export PDF",
            .BackColor = Color.FromArgb(116, 80, 242),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(Me.ClientSize.Width - 130, 16),
            .Width = 100,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        AddHandler exportButton.Click, AddressOf ExportButton_Click
        Me.Controls.Add(exportButton)

        ' Cards for metrics
        cardsFlow = New FlowLayoutPanel() With {
            .Location = New Point(200, 80),
            .Size = New Size(Me.ClientSize.Width - 220, 90),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .BackColor = Color.Transparent
        }
        Me.Controls.Add(cardsFlow)

        ' Create 4 small card panels
        For i As Integer = 1 To 4
            Dim p As New Panel() With {
                .Size = New Size(250, 78),
                .Margin = New Padding(8),
                .BackColor = Color.FromArgb(26, 34, 44),
                .Padding = New Padding(10)
            }
            Dim title As New Label() With {
                .Text = If(i = 1, "Overall average", If(i = 2, "Experiments completed", If(i = 3, "Quizzes passed", "Lab hours logged"))),
                .ForeColor = Color.FromArgb(170, 170, 170),
                .Font = New Font("Segoe UI", 8.5F)
            }
            Dim value As New Label() With {
                .Text = If(i = 1, "85.7%", If(i = 2, "14 / 20", If(i = 3, "9", "23h 40m"))),
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI Semibold", 14.0F, FontStyle.Bold),
                .AutoSize = True,
                .Location = New Point(10, 28)
            }
            p.Controls.Add(title)
            p.Controls.Add(value)
            cardsFlow.Controls.Add(p)
        Next

        ' Charts panel
        chartPanel = New Panel() With {
            .Location = New Point(200, 190),
            .Size = New Size(Me.ClientSize.Width - 220, 250),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        }
        Me.Controls.Add(chartPanel)

        scoreChart = New Chart() With {
            .Dock = DockStyle.Left,
            .Width = CInt((chartPanel.Width - 20) * 0.6),
            .BackColor = Color.FromArgb(22, 28, 35)
        }
        Dim area1 As New ChartArea("ScoreArea")
        area1.BackColor = Color.FromArgb(28, 36, 45)
        area1.AxisX.LabelStyle.ForeColor = Color.FromArgb(180, 180, 180)
        area1.AxisY.LabelStyle.ForeColor = Color.FromArgb(180, 180, 180)
        scoreChart.ChartAreas.Add(area1)
        Dim series1 As New Series("ScoreTrend") With {
            .ChartType = SeriesChartType.Line,
            .Color = Color.FromArgb(76, 175, 80),
            .BorderWidth = 2,
            .ChartArea = "ScoreArea"
        }
        scoreChart.Series.Add(series1)
        Dim title1 As New Title("Score trend") With {
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        }
        scoreChart.Titles.Add(title1)
        chartPanel.Controls.Add(scoreChart)

        masteryChart = New Chart() With {
            .Dock = DockStyle.Right,
            .Width = CInt((chartPanel.Width - 20) * 0.35),
            .BackColor = Color.FromArgb(22, 28, 35)
        }
        Dim area2 As New ChartArea("MasteryArea")
        area2.BackColor = Color.FromArgb(28, 36, 45)
        area2.AxisX.LabelStyle.ForeColor = Color.FromArgb(200, 200, 200)
        area2.AxisY.LabelStyle.ForeColor = Color.FromArgb(200, 200, 200)
        masteryChart.ChartAreas.Add(area2)
        Dim series2 As New Series("Mastery") With {
            .ChartType = SeriesChartType.Column,
            .Color = Color.FromArgb(99, 102, 241),
            .ChartArea = "MasteryArea"
        }
        masteryChart.Series.Add(series2)
        Dim title2 As New Title("Mastery by topic") With {
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        }
        masteryChart.Titles.Add(title2)
        chartPanel.Controls.Add(masteryChart)

        ' DataGridView for history
        historyGrid = New DataGridView() With {
            .Location = New Point(200, chartPanel.Bottom + 16),
            .Size = New Size(Me.ClientSize.Width - 220, Me.ClientSize.Height - chartPanel.Bottom - 40),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right,
            .BackgroundColor = Color.FromArgb(20, 26, 33),
            .ForeColor = Color.White,
            .EnableHeadersVisualStyles = False,
            .ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.FromArgb(30, 36, 44),
                .ForeColor = Color.FromArgb(200, 200, 200),
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
            },
            .RowTemplate = New DataGridViewRow() With {.Height = 28},
            .GridColor = Color.FromArgb(40, 44, 54),
            .AllowUserToAddRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        }
        Me.Controls.Add(historyGrid)

        ' Columns
        historyGrid.Columns.Add("Assessment", "Assessment")
        historyGrid.Columns.Add("Type", "Type")
        historyGrid.Columns.Add("Date", "Date")
        historyGrid.Columns.Add("Score", "Score")
        historyGrid.Columns.Add("Status", "Status")
        Dim viewCol As New DataGridViewLinkColumn() With {
            .Name = "View",
            .HeaderText = "Action",
            .Text = "View report",
            .UseColumnTextForLinkValue = True
        }
        historyGrid.Columns.Add(viewCol)

        ' Resize handling to keep layout consistent
        AddHandler Me.Resize, AddressOf ReportsGrades_Resize
    End Sub

    Private Sub ReportsGrades_Resize(sender As Object, e As EventArgs)
        ' Adjust anchor-based positions for top-right buttons
        exportButton.Location = New Point(Me.ClientSize.Width - 130, 16)
        termCombo.Location = New Point(Me.ClientSize.Width - 240, 20)
        cardsFlow.Size = New Size(Me.ClientSize.Width - 220, 90)
        chartPanel.Size = New Size(Me.ClientSize.Width - 220, 250)
        scoreChart.Width = CInt((chartPanel.Width - 20) * 0.6)
        masteryChart.Width = CInt((chartPanel.Width - 20) * 0.35)
        historyGrid.Size = New Size(Me.ClientSize.Width - 220, Me.ClientSize.Height - chartPanel.Bottom - 40)
    End Sub

    Private Sub PopulateSampleData()
        ' Sample data for score chart: Monthly points
        scoreChart.Series("ScoreTrend").Points.Clear()
        Dim months = New String() {"Mar", "Apr", "May", "Jun", "Jul"}
        Dim values = New Integer() {60, 70, 75, 80, 88}
        For i As Integer = 0 To months.Length - 1
            scoreChart.Series("ScoreTrend").Points.AddXY(months(i), values(i))
        Next
        scoreChart.ChartAreas(0).AxisY.Minimum = 40
        scoreChart.ChartAreas(0).AxisY.Maximum = 100

        ' Sample data for mastery chart
        masteryChart.Series("Mastery").Points.Clear()
        masteryChart.Series("Mastery").Points.AddXY("Acids", 80)
        masteryChart.Series("Mastery").Points.AddXY("Solutions", 55)
        masteryChart.Series("Mastery").Points.AddXY("Redox", 45)
        masteryChart.Series("Mastery").Points.AddXY("Gases", 65)
        masteryChart.Series("Mastery").Points.AddXY("Analysis", 75)
        masteryChart.ChartAreas(0).AxisY.Minimum = 0
        masteryChart.ChartAreas(0).AxisY.Maximum = 100

        ' Sample rows for history grid
        historyGrid.Rows.Clear()
        historyGrid.Rows.Add("Acid & Base Reaction", "Practical", "12 Jul 2026", "92%", "Graded")
        historyGrid.Rows.Add("Precipitation Reaction", "Practical", "18 Jul 2026", "78%", "Graded")
        historyGrid.Rows.Add("Titration Quiz", "Quiz", "21 Jul 2026", "85%", "Graded")
        historyGrid.Rows.Add("Gas Evolution Report", "Report", "24 Jul 2026", "-", "Pending")
        historyGrid.Rows.Add("Flame Test", "Practical", "28 Jul 2026", "88%", "Graded")
    End Sub

    Private Sub ExportButton_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Export PDF clicked - wire up actual export logic here.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub
End Class