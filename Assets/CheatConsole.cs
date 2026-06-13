using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

/*
    IMPORTANTE

    Se quiser adicionar mais comandos é so:

    1 - Referenciar script se necessário
    2 - criar a função do comando que tem de ser void com uma string[] args
    3 - adicionar ao RegisterCommands() o comando e a função
    4 - adicionar ao GetCommandDescription() a descrição do comando para aparecer no /help
*/

public class CheatConsole : MonoBehaviour
{

    [Header("References Scripts")]
    [SerializeField] private ScriptableCredits credits;
    

    [Header("Console UI")]
    public TMP_InputField inputField;
    public GameObject consolePanel;
    public TMP_Text outputText;

    private Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>();

    private List<string> outputHistory = new List<string>();
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;

    private const int MaxHistory = 20;

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        consolePanel.SetActive(false);

        RegisterCommands();
    }

    void RegisterCommands()
    {
        commands.Clear();
        commands.Add("/help", Help);
        commands.Add("/clear", ClearOutput);
        commands.Add("/stonks", Stonks);

        //Aqui adiciono mais comandos
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            bool isActive = !consolePanel.activeSelf;
            consolePanel.SetActive(isActive);

            if (isActive)
            {
                inputField.ActivateInputField();
                inputField.text = "";
                historyIndex = -1;
            }
        }

        if (!consolePanel.activeSelf) return;


        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(inputField.text))
        {
            HandleCommand(inputField.text.Trim());
            inputField.text = "";
            inputField.ActivateInputField();
        }

        //isto aqui serve so para quando escrever um comando e carregar nas setas mostra o que ja escrevi
        upDownArrow();
    }

    void HandleCommand(string input)
    {
        //histórico de comandos
        commandHistory.Insert(0, input);
        if (commandHistory.Count > MaxHistory)
        {
            commandHistory.RemoveAt(commandHistory.Count - 1);
        }

        historyIndex = -1;

        Print($"> {input}", Color.cyan);

        string[] args = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0) return;

        string command = args[0].ToLowerInvariant(); //mete o que escrevi tudo em minusculo para nao dar erro

        if (commands.TryGetValue(command, out Action<string[]> action))
        {
            try
            {
                action.Invoke(args);
            }
            catch (Exception ex)
            {
                Print($"Erro ao executar comando: {ex.Message}", Color.red);
            }
        }
        else
        {
            Print($"Comando desconhecido: {command}", Color.yellow);
            Print("Digite /help para ver os comandos disponíveis.", Color.gray);
        }
    }

    

    

    void Help(string[] args)
    {
        Print("──────────────────────────────", Color.white);
        Print("Comandos disponíveis:", Color.white);

        foreach (var cmd in commands.Keys)
        {
            string description = GetCommandDescription(cmd);
            Print($"  {cmd} {description}", Color.gray);
        }

        Print("──────────────────────────────", Color.white);
    }

    void ClearOutput(string[] args)
    {
        outputHistory.Clear();
        outputText.text = "";
        Print("Consola limpa.", Color.gray);
    }

    void Stonks(string[] args) 
    {
        Print("Créditos no topo", Color.green);
        credits.AddCredits(9999);
    }

    string GetCommandDescription(string command)
    {
        //aqui é para adicionar descrições para cada comando, que será mostrado no /help
        switch (command)
        {
            case "/help":
                return "- Mostra esta ajuda";
            case "/clear":
                return "- Limpa a consola";
            case "/stonks":
                return "- Adiciona 9999 créditos";
            default:
                return "";
        }
    }


    void Print(string message, Color? color = null)
    {
        string colorMessage;

        if (color.HasValue)
        {
            //isto aqui serve para transformar a cor em hexadecimal para usar na tag de cor do TMPro
            string corHex = ColorUtility.ToHtmlStringRGB(color.Value);
            colorMessage = $"<color=#{corHex}>{message}</color>";
        }
        else
        {
            colorMessage = message;
        }

        outputHistory.Add(colorMessage);

        if (outputHistory.Count > MaxHistory)
        {
            outputHistory.RemoveAt(0);
        }

        outputText.text = string.Join("\n", outputHistory);
    }

    //nao precisava disto so meti porque sim, isto so serve para usar as setas do teclado para ver o que escrevi anteriormente
    void upDownArrow()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (commandHistory.Count > 0)
            {
                historyIndex = Mathf.Clamp(historyIndex + 1, 0, commandHistory.Count - 1);
                inputField.text = commandHistory[historyIndex];
                inputField.caretPosition = inputField.text.Length;
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (commandHistory.Count > 0)
            {
                historyIndex = Mathf.Clamp(historyIndex - 1, -1, commandHistory.Count - 1);
                inputField.text = (historyIndex >= 0) ? commandHistory[historyIndex] : "";
                inputField.caretPosition = inputField.text.Length;
            }
        }
    }
}
