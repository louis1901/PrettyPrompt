using PrettyPrompt;
using PrettyPrompt.Completion;
using PrettyPrompt.Highlighting;
using System.Diagnostics;
using System.IO.Ports;

namespace ConsoleApp1;
public class Program
{
    static bool _continue;
    static SerialPort _serialPort;

    public static async Task Main()
    {
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

        string name;
        string message;
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        Thread readThread = new Thread(Read);

        // Create a new SerialPort object with default settings.
        _serialPort = new SerialPort();

        // Allow the user to set the appropriate properties.
        //_serialPort.PortName = SetPortName(_serialPort.PortName);
        //_serialPort.BaudRate = SetPortBaudRate(_serialPort.BaudRate);
        //_serialPort.Parity = SetPortParity(_serialPort.Parity);
        //_serialPort.DataBits = SetPortDataBits(_serialPort.DataBits);
        //_serialPort.StopBits = SetPortStopBits(_serialPort.StopBits);
        //_serialPort.Handshake = SetPortHandshake(_serialPort.Handshake);

        _serialPort.PortName = "COM5";
        _serialPort.BaudRate = 115200;
        _serialPort.Parity = Parity.None;
        _serialPort.DataBits = 8;
        _serialPort.StopBits = StopBits.One;
        _serialPort.Handshake = Handshake.None;

        // Set the read/write timeouts
        _serialPort.ReadTimeout = 500;
        _serialPort.WriteTimeout = 500;

        _serialPort.Open();
        _continue = true;
        readThread.Start();

        Console.Write("Name: ");
        name = Console.ReadLine();

        Console.WriteLine("Type QUIT to exit");

        var prompt = new Prompt(persistentHistoryFilepath: "./history-file", new PromptCallbacks
        {
            // populate completions and documentation for autocompletion window
            CompletionCallback = FindCompletions,
            // defines syntax highlighting
            HighlightCallback = Highlight,
            // registers functions to be called when the user presses a key. The text
            // currently typed into the prompt, along with the caret position within
            // that text are provided as callback parameters.
            KeyPressCallbacks =
                {
                    [(ConsoleModifiers.Control, ConsoleKey.F1)] = ShowFruitDocumentation // could also just provide a ConsoleKey, instead of a tuple.
                }
        });

        while (_continue)
        {
            var response = await prompt.ReadLineAsync().ConfigureAwait(false);
            if (response.IsSuccess)
            {
                if (stringComparer.Equals("quit", response.Text))
                {
                    _continue = false;
                }
                else
                {
                    _serialPort.WriteLine(response.Text);
                }
                // optionally, use response.CancellationToken so the user can
                // cancel long-running processing of their response via ctrl-c
                //Console.WriteLine("You wrote " + (response.IsHardEnter ? response.Text.ToUpper() : response.Text));
            }

            //message = Console.ReadLine();

            //if (stringComparer.Equals("quit", message))
            //{
            //    _continue = false;
            //}
            //else
            //{
            //    _serialPort.WriteLine(
            //        String.Format("<{0}>: {1}", name, message));
            //}
        }

        readThread.Join();

    }

    private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        _serialPort?.Close();
        Console.WriteLine("serial port close.");
    }

    public static void Read()
    {
        while (_continue)
        {
            try
            {
                string message = _serialPort.ReadLine();
                Console.WriteLine(message);
            }
            catch (TimeoutException) { }
        }
    }

    #region Serial Port Config
    // Display Port values and prompt user to enter a port.
    public static string SetPortName(string defaultPortName)
    {
        string portName;

        Console.WriteLine("Available Ports:");
        foreach (string s in SerialPort.GetPortNames())
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Enter COM port value (Default: {0}): ", defaultPortName);
        portName = Console.ReadLine();

        if (portName == "" || !(portName.ToLower()).StartsWith("com"))
        {
            portName = defaultPortName;
        }
        return portName;
    }
    // Display BaudRate values and prompt user to enter a value.
    public static int SetPortBaudRate(int defaultPortBaudRate)
    {
        string baudRate;

        Console.Write("Baud Rate(default:{0}): ", defaultPortBaudRate);
        baudRate = Console.ReadLine();

        if (baudRate == "")
        {
            baudRate = defaultPortBaudRate.ToString();
        }

        return int.Parse(baudRate);
    }

    // Display PortParity values and prompt user to enter a value.
    public static Parity SetPortParity(Parity defaultPortParity)
    {
        string parity;

        Console.WriteLine("Available Parity options:");
        foreach (string s in Enum.GetNames(typeof(Parity)))
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Enter Parity value (Default: {0}):", defaultPortParity.ToString(), true);
        parity = Console.ReadLine();

        if (parity == "")
        {
            parity = defaultPortParity.ToString();
        }

        return (Parity)Enum.Parse(typeof(Parity), parity, true);
    }
    // Display DataBits values and prompt user to enter a value.
    public static int SetPortDataBits(int defaultPortDataBits)
    {
        string dataBits;

        Console.Write("Enter DataBits value (Default: {0}): ", defaultPortDataBits);
        dataBits = Console.ReadLine();

        if (dataBits == "")
        {
            dataBits = defaultPortDataBits.ToString();
        }

        return int.Parse(dataBits.ToUpperInvariant());
    }

    // Display StopBits values and prompt user to enter a value.
    public static StopBits SetPortStopBits(StopBits defaultPortStopBits)
    {
        string stopBits;

        Console.WriteLine("Available StopBits options:");
        foreach (string s in Enum.GetNames(typeof(StopBits)))
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Enter StopBits value (None is not supported and \n" +
         "raises an ArgumentOutOfRangeException. \n (Default: {0}):", defaultPortStopBits.ToString());
        stopBits = Console.ReadLine();

        if (stopBits == "")
        {
            stopBits = defaultPortStopBits.ToString();
        }

        return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
    }
    public static Handshake SetPortHandshake(Handshake defaultPortHandshake)
    {
        string handshake;

        Console.WriteLine("Available Handshake options:");
        foreach (string s in Enum.GetNames(typeof(Handshake)))
        {
            Console.WriteLine("   {0}", s);
        }

        Console.Write("Enter Handshake value (Default: {0}):", defaultPortHandshake.ToString());
        handshake = Console.ReadLine();

        if (handshake == "")
        {
            handshake = defaultPortHandshake.ToString();
        }

        return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
    }
    #endregion

    #region PrettyPrompt
    // demo data
    private static readonly (string Name, string Description, AnsiColor Highlight)[] Fruits = new[]
    {
            ( "apple", "the round fruit of a tree of the rose family, which typically has thin red or green skin and crisp flesh. Many varieties have been developed as dessert or cooking fruit or for making cider.", AnsiColor.BrightRed ),
            ( "apricot", "a juicy, soft fruit, resembling a small peach, of an orange-yellow color.", AnsiColor.Yellow ),
            ( "avocado", "a pear-shaped fruit with a rough leathery skin, smooth oily edible flesh, and a large stone.", AnsiColor.Green ),
            ( "banana", "a long curved fruit which grows in clusters and has soft pulpy flesh and yellow skin when ripe.", AnsiColor.BrightYellow ),
            ( "cantaloupe", "a small round melon of a variety with orange flesh and ribbed skin.", AnsiColor.Green ),
            ( "grapefruit", "a large round yellow citrus fruit with an acid juicy pulp.", AnsiColor.RGB(224, 112, 124) ),
            ( "grape", "a berry, typically green (classified as white), purple, red, or black, growing in clusters on a grapevine, eaten as fruit, and used in making wine.", AnsiColor.Blue ),
            ( "mango", "a fleshy, oval, yellowish-red tropical fruit that is eaten ripe or used green for pickles or chutneys.", AnsiColor.Yellow ),
            ( "melon", "the large round fruit of a plant of the gourd family, with sweet pulpy flesh and many seeds.", AnsiColor.Green ),
            ( "orange", "a round juicy citrus fruit with a tough bright reddish-yellow rind.", AnsiColor.RGB(255, 165, 0) ),
            ( "pear", "a yellowish- or brownish-green edible fruit that is typically narrow at the stalk and wider toward the base, with sweet, slightly gritty flesh.", AnsiColor.Green ),
            ( "peach", "a round stone fruit with juicy yellow flesh and downy pinkish-yellow skin.", AnsiColor.RGB(255, 229, 180) ),
            ( "pineapple", "a large juicy tropical fruit consisting of aromatic edible yellow flesh surrounded by a tough segmented skin and topped with a tuft of stiff leaves.", AnsiColor.BrightYellow ),
            ( "strawberry", "a sweet soft red fruit with a seed-studded surface.", AnsiColor.BrightRed ),
        };

    private static readonly (string Name, AnsiColor Color)[] ColorsToHighlight = new[]
    {
            ("red", AnsiColor.Red),
            ("green", AnsiColor.Green),
            ("yellow", AnsiColor.Yellow),
            ("blue", AnsiColor.Blue),
            ("purple", AnsiColor.RGB(72, 0, 255)),
            ("orange", AnsiColor.RGB(255, 165, 0) ),
            ("root", AnsiColor.Red),
            ("$", AnsiColor.Blue)
        };

    // demo completion algorithm callback
    private static Task<IReadOnlyList<CompletionItem>> FindCompletions(string typedInput, int caret)
    {
        var textUntilCaret = typedInput.Substring(0, caret);
        var previousWordStart = textUntilCaret.LastIndexOfAny(new[] { ' ', '\n', '.', '(', ')' });
        var typedWord = previousWordStart == -1
            ? textUntilCaret.ToLower()
            : textUntilCaret.Substring(previousWordStart + 1).ToLower();
        return Task.FromResult<IReadOnlyList<CompletionItem>>(
            Fruits
            .Where(fruit => fruit.Name.StartsWith(typedWord))
            .Select(fruit =>
            {
                var displayText = new FormattedString(fruit.Name, new FormatSpan(0, fruit.Name.Length, new ConsoleFormat(Foreground: fruit.Highlight)));
                var description = GetFormattedString(fruit.Description);
                return new CompletionItem
                {
                    StartIndex = previousWordStart + 1,
                    ReplacementText = fruit.Name,
                    DisplayText = displayText,
                    ExtendedDescription = new Lazy<Task<FormattedString>>(() => Task.FromResult(description))
                };
            })
            .ToArray()
        );
    }

    // demo syntax highlighting callback
    private static Task<IReadOnlyCollection<FormatSpan>> Highlight(string text)
    {
        IReadOnlyCollection<FormatSpan> spans = EnumerateFormatSpans(text, Fruits.Select(f => (f.Name, f.Highlight))).ToList();
        return Task.FromResult(spans);
    }

    private static FormattedString GetFormattedString(string text)
        => new(text, EnumerateFormatSpans(text, ColorsToHighlight));

    private static IEnumerable<FormatSpan> EnumerateFormatSpans(string text, IEnumerable<(string TextToFormat, AnsiColor Color)> formattingInfo)
    {
        foreach (var (textToFormat, color) in formattingInfo)
        {
            int startIndex;
            int offset = 0;
            while ((startIndex = text.AsSpan(offset).IndexOf(textToFormat)) != -1)
            {
                yield return new FormatSpan(offset + startIndex, textToFormat.Length, new ConsoleFormat(Foreground: color));
                offset += startIndex + textToFormat.Length;
            }
        }
    }

    private static Task<KeyPressCallbackResult> ShowFruitDocumentation(string text, int caret)
    {
        string wordUnderCursor = GetWordAtCaret(text, caret).ToLower();

        if (Fruits.Any(f => f.Name.ToLower() == wordUnderCursor))
        {
            // wikipedia is the definitive fruit documentation.
            LaunchBrowser("https://en.wikipedia.org/wiki/" + Uri.EscapeUriString(wordUnderCursor));
        }

        // since we return a null KeyPressCallbackResult here, the user will remain on the current prompt
        // and will still be able to edit the input.
        // if we were to return a non-null result, this result will be returned from ReadLineAsync(). This
        // is useful if we want our custom keypress to submit the prompt and control the output manually.
        return Task.FromResult<KeyPressCallbackResult>(null);

        // local functions
        static string GetWordAtCaret(string text, int caret)
        {
            var words = text.Split(new[] { ' ', '\n' });
            string wordAtCaret = string.Empty;
            int currentIndex = 0;
            foreach (var word in words)
            {
                if (currentIndex < caret && caret < currentIndex + word.Length)
                {
                    wordAtCaret = word;
                    break;
                }
                currentIndex += word.Length + 1; // +1 due to word separator
            }

            return wordAtCaret;
        }

        static void LaunchBrowser(string url)
        {
            var browser =
                OperatingSystem.IsWindows() ? new ProcessStartInfo("explorer", $"{url}") : // using cmd will cancel TreatControlCAsInput. We don't want that.
                OperatingSystem.IsMacOS() ? new ProcessStartInfo("open", url) :
                new ProcessStartInfo("xdg-open", url); //linux, unix-like

            Process.Start(browser).WaitForExit();
        }
    }
    #endregion
}
