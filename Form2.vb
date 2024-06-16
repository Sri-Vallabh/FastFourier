Imports System.IO
Imports System.Diagnostics
Imports System.Windows.Forms
Imports OxyPlot
Imports OxyPlot.Series
Imports OxyPlot.WindowsForms
Imports System.Threading
Imports System.Threading.Tasks

Public Class Form2
    Private Const sampleRate As Integer = 22050
    Private flowLayoutPanel As FlowLayoutPanel
    Private plotView As PlotView
    Private splitContainer As SplitContainer
    Private toggleButton As Button
    Private showNyquist As Boolean = True
    Private selectedFilePath As String = ""
    Private buttonToForm1 As Button
    Private expectedTimeLabel As Label
    Private stopwatchLabel As Label
    Private stopwatch As Stopwatch
    Private plotTimer As System.Windows.Forms.Timer

    Private cts As CancellationTokenSource

    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Maximize the form to start in full screen mode
        Me.WindowState = FormWindowState.Maximized

        ' Initialize SplitContainer
        splitContainer = New SplitContainer With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterDistance = 20,
            .Padding = New Padding(10)
        }

        ' Initialize FlowLayoutPanel and PlotView
        flowLayoutPanel = New FlowLayoutPanel With {
            .Dock = DockStyle.Left,
            .AutoScroll = True
        }
        plotView = New PlotView With {
            .Dock = DockStyle.Fill
        }

        ' Initialize Toggle Button
        toggleButton = New Button With {
            .Text = "Show Full Spectrum",
            .Dock = DockStyle.Top,
            .Height = 30
        }
        AddHandler toggleButton.Click, AddressOf ToggleButton_Click

        ' Initialize Button to open Form1
        buttonToForm1 = New Button With {
            .Text = "Change to FFT",
            .Dock = DockStyle.Top,
            .Height = 30
        }
        AddHandler buttonToForm1.Click, AddressOf ButtonToForm1_Click

        ' Initialize Expected Time Label
        expectedTimeLabel = New Label With {
            .Text = "Expected time on x86 : 0.0000 s",
            .Dock = DockStyle.Top,
            .Height = 30,
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' Initialize Stopwatch Label
        stopwatchLabel = New Label With {
            .Text = "Time taken: 0.0000 s",
            .Dock = DockStyle.Top,
            .Height = 30,
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' Add FlowLayoutPanel, Toggle Button, Expected Time Label, Stopwatch Label, and ButtonToForm1 to SplitContainer Panel1
        splitContainer.Panel1.Controls.Add(flowLayoutPanel)
        splitContainer.Panel1.Controls.Add(toggleButton)
        splitContainer.Panel1.Controls.Add(expectedTimeLabel)
        splitContainer.Panel1.Controls.Add(stopwatchLabel)
        splitContainer.Panel1.Controls.Add(buttonToForm1)

        ' Add PlotView to SplitContainer Panel2
        splitContainer.Panel2.Controls.Add(plotView)

        ' Add SplitContainer to the Form
        Me.Controls.Add(splitContainer)

        ' Get the current directory
        Dim currentDirectory As String = My.Computer.FileSystem.CurrentDirectory
        ' Navigate three directories up
        Dim folderPath As String = Path.GetFullPath(Path.Combine(currentDirectory, "Vowel_recordings"))

        ' Check if the folder exists
        If Not Directory.Exists(folderPath) Then
            MessageBox.Show("Folder does not exist: " & folderPath)
            Exit Sub
        End If

        ' Get the text files in the folder
        Dim textFiles() As String = Directory.GetFiles(folderPath, "*.txt")

        ' Create a button for each text file
        For Each filePath As String In textFiles
            Dim fileName As String = Path.GetFileName(filePath)
            Dim button As New Button With {
                .Text = fileName,
                .Tag = filePath,
                .Width = 180,
                .Height = 30
            }
            AddHandler button.Click, AddressOf FileButton_Click
            flowLayoutPanel.Controls.Add(button)
        Next
    End Sub

    Private Sub ToggleButton_Click(sender As Object, e As EventArgs)
        showNyquist = Not showNyquist
        If showNyquist Then
            toggleButton.Text = "Show Full Spectrum"
        Else
            toggleButton.Text = "Show Up to Nyquist"
        End If
        ' Refresh the plot
        RefreshPlot()
    End Sub

    Private Sub FileButton_Click(sender As Object, e As EventArgs)
        ' Update the selected file and refresh the plot
        selectedFilePath = CType(sender, Button).Tag.ToString()
        RefreshPlot()
    End Sub

    Private Async Sub RefreshPlot()
        If String.IsNullOrEmpty(selectedFilePath) Then
            Return
        End If

        Dim filePath As String = selectedFilePath

        ' Read all lines from the file and convert to Double
        Dim lines() As String = File.ReadAllLines(filePath)
        Dim samples(lines.Length - 1) As Double
        For i As Integer = 0 To lines.Length - 1
            samples(i) = Convert.ToDouble(lines(i))
        Next

        ' Calculate the expected time based on the given formula
        Dim n As Integer = lines.Length
        Dim a As Double
        If showNyquist Then
            a = 1.6153487790483 * Math.Pow(10, -7)
        Else
            a = 2 * 1.6153487790483 * Math.Pow(10, -7)
        End If
        Dim b As Double = -0.00158476827409931
        Dim c As Double = 10.1600099591547
        Dim expectedTime As Double = a * n * n + b * n + c

        ' Display the expected time
        expectedTimeLabel.Text = "Expected time on x86 : " & expectedTime.ToString("F4") & " s"

        ' Start the stopwatch
        stopwatch = Stopwatch.StartNew()

        ' Initialize and start the timer
        plotTimer = New System.Windows.Forms.Timer()
        AddHandler plotTimer.Tick, AddressOf UpdateStopwatchLabel
        plotTimer.Interval = 1000 ' 1 second
        plotTimer.Start()

        ' Create a cancellation token source and set a timeout
        cts = New CancellationTokenSource()
        cts.CancelAfter(TimeSpan.FromSeconds(22))

        Try
            ' Perform DFT asynchronously to avoid freezing the UI
            Dim dftResults As Complex() = Await Task.Run(Function() DFT(samples, cts.Token), cts.Token)

            ' Compute magnitudes
            Dim magnitudes(dftResults.Length - 1) As Double
            For i As Integer = 0 To dftResults.Length - 1
                magnitudes(i) = dftResults(i).Magnitude()
            Next

            ' Determine the range to display
            Dim displayLength As Integer
            If showNyquist Then
                displayLength = magnitudes.Length \ 2
            Else
                displayLength = magnitudes.Length
            End If
            Console.WriteLine("Length of samples array: " & samples.Length)
            Console.WriteLine("Length of magnitudes array: " & magnitudes.Length)

            ' Prepare data for plotting
            Dim frequencies(displayLength - 1) As Double
            For i As Integer = 0 To displayLength - 1
                frequencies(i) = i * sampleRate / magnitudes.Length
            Next
            Console.WriteLine("final freq: " & frequencies(displayLength - 1))

            ' Update the UI with the plot
            ' Stop the stopwatch and timer
            stopwatch.Stop()
            plotTimer.Stop()
            plotTimer.Dispose()

            ' Update the label with the final time
            stopwatchLabel.Text = "Time taken: " & stopwatch.Elapsed.TotalSeconds.ToString("F4") & " s"

            ' Plot the frequency domain
            Dim plotModel As New PlotModel With {.Title = "DFT Frequency Domain - " & Path.GetFileName(filePath)}
            Dim lineSeries As New LineSeries
            For i As Integer = 0 To displayLength - 1
                lineSeries.Points.Add(New DataPoint(frequencies(i), magnitudes(i)))
            Next
            plotModel.Series.Add(lineSeries)
            plotView.Model = plotModel

        Catch ex As OperationCanceledException
            ' Handle the case when the DFT computation is canceled due to timeout
            MessageBox.Show("DFT computation was canceled due to timeout.")
        Finally
            If cts IsNot Nothing Then
                cts.Dispose()
            End If
        End Try
    End Sub

    Private Sub UpdateStopwatchLabel(sender As Object, e As EventArgs)
        ' Update the stopwatch label with the current elapsed time
        stopwatchLabel.Text = "Time taken: " & stopwatch.Elapsed.TotalSeconds.ToString("F4") & " s"
    End Sub

    Private Function DFT(samples() As Double, token As CancellationToken) As Complex()
        Dim n As Integer = samples.Length
        Dim result(n - 1) As Complex

        For k As Integer = 0 To n - 1
            token.ThrowIfCancellationRequested() ' Check for cancellation
            Dim sum As New Complex(0, 0)
            For t As Integer = 0 To n - 1
                Dim exponent As Complex = Complex.Exp(New Complex(0, -2 * Math.PI * t * k / n))
                sum += New Complex(samples(t), 0) * exponent
            Next
            result(k) = sum
        Next

        Return result
    End Function

    Private Sub ButtonToForm1_Click(sender As Object, e As EventArgs)
        ' Open Form1 and close Form2
        Dim form1 As New Form1()
        form1.Show()
        Me.Hide()
    End Sub
End Class
