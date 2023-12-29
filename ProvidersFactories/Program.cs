using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Data.Common;
using ProvidersFactories.Controls;

internal class Program
{
    private static readonly string sqliteFile = "fruits_and_vegetables.sqlite";
    private const string PROVIDER_INVARIANT_NAME = "Microsoft.Data.Sqlite";
    private static DbProviderFactory dbProviderFactory;
    private static DbConnection connection;
    private enum MenuItems
    {
        Додавання,
        Редагування,
        Видалення,
        Вихід,
    }
    private enum FruitOrVegetable
    {
        Фрукт,
        Овоч,
    }

    private enum ItemsProperties
    {
        Назва,
        Тип,
        Колір,
        Калорії,
    }

    private static async Task Main(string[] args)
    {
        dbProviderFactory = SqliteFactory.Instance;

        Console.OutputEncoding = System.Text.Encoding.UTF8;

        await CreateTableAsync();
        await CreateItemsAsync();
        await ReadAndDisplayAllAsync("SELECT * FROM fruits_and_vegetables", "Усі дані про фрукти та овочі");
        await ReadAndDisplayAllAsync("SELECT name FROM fruits_and_vegetables", "Всі назви овочів і фруктів");
        await ReadAndDisplayAllAsync("SELECT color FROM fruits_and_vegetables", "Всі назви кольорів");
        await ReadAndDisplayAllAsync("SELECT MIN(calorie) FROM fruits_and_vegetables", "Мінімальна калорійність");
        await ReadAndDisplayAllAsync("SELECT MAX(calorie) FROM fruits_and_vegetables", "Максимальна калорійність");
        await ReadAndDisplayAllAsync("SELECT AVG(calorie) FROM fruits_and_vegetables", "Середня калорійність");
        await ReadAndDisplayAllAsync("SELECT COUNT(*) FROM fruits_and_vegetables WHERE type =\"Овоч\" ", "Кількість овочів");
        await ReadAndDisplayAllAsync("SELECT COUNT(*) FROM fruits_and_vegetables WHERE type =\"Фрукт\" ", "Кількість фруктів");
        await ReadAndDisplayAllAsync("SELECT COUNT(*) FROM fruits_and_vegetables WHERE color =\"Білий\" ", "Кількість овочів і фруктів білого кольору");
        await ReadAndDisplayAllAsync("SELECT color,  COUNT(*) AS count FROM fruits_and_vegetables GROUP BY color", "Кількість овочів і фруктів кожного кольору");
        await ReadAndDisplayAllAsync("SELECT name, calorie FROM fruits_and_vegetables WHERE calorie < 50", "Овочі і фрукти з калорійністю нижче 50");
        await ReadAndDisplayAllAsync("SELECT name, calorie FROM fruits_and_vegetables WHERE calorie > 50", "Овочі і фрукти з калорійністю вище 50");
        await ReadAndDisplayAllAsync("SELECT name, calorie FROM fruits_and_vegetables WHERE calorie > 30 AND calorie < 70", "Овочі і фрукти з калорійністю в діапазоні від 30 до 70");
        await ReadAndDisplayAllAsync("SELECT name FROM fruits_and_vegetables WHERE color =\"Червоний\" ", "Овочі і фрукти червоного кольору");
        await ReadAndDisplayAllAsync("SELECT name FROM fruits_and_vegetables WHERE color =\"Жовтий\" ", "Овочі і фрукти жовтого кольору");

        Console.WriteLine("Виберіть що ви хочете зробити *управління за допомогою стрілочок*");
        while (true)
        {
            int input = Menu.MultipleChoice(true, new MenuItems());
            switch ((MenuItems)input)
            {
                case MenuItems.Додавання:
                    Console.Clear();
                    await InsertNewRowAsync();
                    break;

                case MenuItems.Редагування:
                    Console.Clear();
                    await UpdateRowAsync();
                    break;

                case MenuItems.Видалення:
                    Console.Clear();
                    await DeleteRowAsync();
                    break;

                case MenuItems.Вихід:
                    Console.Clear();
                    Environment.Exit(0);
                    break;

                default:
                    break;
            }
            await ReadAndDisplayAllAsync("SELECT * FROM fruits_and_vegetables", "Усі дані про фрукти та овочі");
        }
    }

    private static async Task CreateTableAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        connection = dbProviderFactory.CreateConnection();
        connection.ConnectionString = $"Data Source={sqliteFile}";
        await connection.OpenAsync();

        var createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS fruits_and_vegetables (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,
                Type TEXT,
                Color TEXT,
                Calorie INTEGER
            )";
        await createTableCommand.ExecuteNonQueryAsync();

        connection.Close();

        stopwatch.Stop();
        Console.WriteLine($"Час створення бд: {stopwatch.Elapsed.TotalMilliseconds} мс");
        Console.ReadKey();
        Console.Clear();
    }

    private static async Task CreateItemsAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        connection = dbProviderFactory.CreateConnection();
        connection.ConnectionString = $"Data Source={sqliteFile}";
        await connection.OpenAsync();

        string[,] items = {
        {"Яблуко", "Фрукт", "Червоний", "52"},
        {"Апельсин", "Фрукт", "Помаранчевий", "47"},
        {"Банан", "Фрукт", "Жовтий", "89"},
        {"Авокадо", "Фрукт", "Зелений", "160"},
        {"Груша", "Фрукт", "Жовто-зелений", "57"},
        {"Картопля", "Овоч", "Коричневий", "77"},
        {"Томат", "Овоч", "Червоний", "18"},
        {"Буряк", "Овоч", "Фіолетовий", "45"},
        {"Морква", "Овоч", "Помаранчевий", "41"},
        {"Цибуля", "Овоч", "Білий", "40"},
        {"Грейпфрут", "Фрукт", "Рожевий", "42"},
        {"Мандарин", "Фрукт", "Оранжевий", "53"},
        {"Кавун", "Фрукт", "Зелений", "30"},
        {"Патисон", "Овоч", "Білий", "18"},
        {"Брокколі", "Овоч", "Зелений", "34"},
        {"Айва", "Фрукт", "Жовто-зелений", "43"}
        };

        using var transaction = connection.BeginTransaction();
        for (int i = 0; i < items.GetLength(0); i++)
        {
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO fruits_and_vegetables (name, type, color, calorie) VALUES (@name, @type, @color, @calorie)";

            string name = items[i, 0];
            string type = items[i, 1];
            string color = items[i, 2];
            int calorie = int.Parse(items[i, 3]);

            var paramName = insertCommand.CreateParameter();
            paramName.ParameterName = "@name";
            paramName.Value = name;
            insertCommand.Parameters.Add(paramName);

            var paramType = insertCommand.CreateParameter();
            paramType.ParameterName = "@type";
            paramType.Value = type;
            insertCommand.Parameters.Add(paramType);

            var paramColor = insertCommand.CreateParameter();
            paramColor.ParameterName = "@color";
            paramColor.Value = color;
            insertCommand.Parameters.Add(paramColor);

            var paramCalorie = insertCommand.CreateParameter();
            paramCalorie.ParameterName = "@calorie";
            paramCalorie.Value = calorie;
            insertCommand.Parameters.Add(paramCalorie);

            await insertCommand.ExecuteNonQueryAsync();
        }

        transaction.Commit();

        stopwatch.Stop();
        Console.WriteLine($"Час заповнення бд: {stopwatch.Elapsed.TotalMilliseconds} мс");
        Console.ReadKey();
        Console.Clear();
    }

    private static async Task ReadAndDisplayAllAsync(string comm, string displayMessage)
    {
        var stopwatch = Stopwatch.StartNew();

        connection = dbProviderFactory.CreateConnection();
        connection.ConnectionString = $"Data Source={sqliteFile}";
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = comm;

        using var reader = await command.ExecuteReaderAsync();
        Console.WriteLine(displayMessage);
        Console.WriteLine();

        while (await reader.ReadAsync())
        {
            string output = "";
            for (int i = 0; i < reader.FieldCount; i++)
            {
                output += $"{reader.GetString(i),-10}\t";
            }
            Console.WriteLine(output);
        }

        stopwatch.Stop();
        Console.WriteLine($"\nЧас запиту: {stopwatch.Elapsed.TotalMilliseconds} мс");
        Console.ReadKey();
        Console.Clear();
    }

    private static async Task InsertNewRowAsync()
    {
        Console.WriteLine("Введіть ім'я:");
        string name = Console.ReadLine();
        Console.Clear();

        Console.WriteLine("Виберіть тип:");
        int inputType = Menu.MultipleChoice(true, new FruitOrVegetable());
        string type = "";
        switch (inputType)
        {
            case (int)FruitOrVegetable.Фрукт:
                type = "Фрукт";
                break;
            case (int)FruitOrVegetable.Овоч:
                type = "Овоч";
                break;
            default:
                break;
        }

        Console.Clear();

        Console.WriteLine("Введіть колір:");
        string color = Console.ReadLine();
        Console.Clear();

        int calorie;
        bool isValidInput;
        Console.WriteLine("Введіть кількість калорій:");
        do
        {
            string input = Console.ReadLine();

            isValidInput = int.TryParse(input, out calorie);

            if (!int.TryParse(input, out calorie))
            {
                Console.WriteLine("Будь ласка, введіть числове значення.");

            }
        } while (!isValidInput);

        var stopwatch = Stopwatch.StartNew();
        
        connection = dbProviderFactory.CreateConnection();
        connection.ConnectionString = $"Data Source={sqliteFile}";
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO fruits_and_vegetables (name, type, color, calorie) VALUES (@name, @type, @color, @calorie)";

        var paramName = command.CreateParameter();
        paramName.ParameterName = "@name";
        paramName.Value = name;
        command.Parameters.Add(paramName);

        var paramType = command.CreateParameter();
        paramType.ParameterName = "@type";
        paramType.Value = type;
        command.Parameters.Add(paramType);

        var paramColor = command.CreateParameter();
        paramColor.ParameterName = "@color";
        paramColor.Value = color;
        command.Parameters.Add(paramColor);

        var paramCalorie = command.CreateParameter();
        paramCalorie.ParameterName = "@calorie";
        paramCalorie.Value = calorie;
        command.Parameters.Add(paramCalorie);

        await command.ExecuteNonQueryAsync();

        stopwatch.Stop();
        Console.WriteLine($"\nЧас створення {name}: {stopwatch.Elapsed.TotalMilliseconds} мс");
        Console.ReadKey();
        Console.Clear();
    }

    private static async Task DeleteRowAsync()
    {
        Console.WriteLine("Введіть назву фрукта/овоча, який ви хочете видалити:");
        string nameToDelete = Console.ReadLine();
        Console.Clear();

        var stopwatch = Stopwatch.StartNew();
        connection = dbProviderFactory.CreateConnection();
        connection.ConnectionString = $"Data Source={sqliteFile}";
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.Connection = connection;
        command.CommandText = "DELETE FROM fruits_and_vegetables WHERE name = @nameToDelete";

        DbParameter nameToDeleteParam = command.CreateParameter();
        nameToDeleteParam.ParameterName = "@nameToDelete";
        nameToDeleteParam.Value = nameToDelete;
        command.Parameters.Add(nameToDeleteParam);

        await command.ExecuteNonQueryAsync();
        stopwatch.Stop();
        Console.WriteLine($"\nЧас видалення {nameToDelete}: {stopwatch.Elapsed.TotalMilliseconds} мс");
        Console.ReadKey();
        Console.Clear();
    }

    private static async Task UpdateRowAsync()
    {
        string nameToUpdate = "";
        bool exists = false;

        do
        {
            Console.WriteLine("Введіть назву фрукта/овоча, який ви хочете відредагувати:");
            nameToUpdate = Console.ReadLine();
            Console.Clear();

            exists = await CheckIfExistsAsync(nameToUpdate);

            if (!exists)
            {
                Console.WriteLine($"Запис {nameToUpdate} не знайдено в базі даних. Спробуйте ще раз.");
            }

        } while (!exists);

        string attributeName = "";
        var newValue = "";
        Console.WriteLine("Виберіть що хочете змінити:");
        int input = Menu.MultipleChoice(true, new ItemsProperties());
        switch (input)
        {
            case (int)ItemsProperties.Назва:
                Console.Clear();
                attributeName = "name";
                Console.WriteLine("Введіть нову назву:");
                newValue = Console.ReadLine();
                break;

            case (int)ItemsProperties.Тип:
                Console.Clear();
                int inputType = Menu.MultipleChoice(true, new FruitOrVegetable());
                attributeName = "type";
                switch (inputType)
                {
                    case (int)FruitOrVegetable.Фрукт:
                        newValue = "Фрукт";
                        break;
                    case (int)FruitOrVegetable.Овоч:
                        newValue = "Овоч";
                        break;
                    default:
                        break;
                }
                break;

            case (int)ItemsProperties.Колір:
                Console.Clear();
                attributeName = "color";
                Console.WriteLine("Введіть новий колір:");
                newValue = Console.ReadLine();
                break;

            case (int)ItemsProperties.Калорії:
                Console.Clear();
                attributeName = "calorie";
                bool isValidInput;
                int calorie;
                Console.WriteLine("Введіть кількість калорій:");
                do
                {
                    string inputcal = Console.ReadLine();

                    isValidInput = int.TryParse(inputcal, out calorie);

                    if (!int.TryParse(inputcal, out calorie))
                    {
                        Console.WriteLine("Будь ласка, введіть числове значення.");

                    }
                } while (!isValidInput);
                newValue = calorie.ToString();
                break;
            default:
                break;
        }
        var stopwatch = Stopwatch.StartNew();
        connection = dbProviderFactory.CreateConnection();
        connection.ConnectionString = $"Data Source={sqliteFile}";
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.Connection = connection;
        command.CommandText = $"UPDATE fruits_and_vegetables SET {attributeName} = @newValue WHERE name = @nameToUpdate";

        DbParameter newValueParam = command.CreateParameter();
        newValueParam.ParameterName = "@newValue";
        newValueParam.Value = newValue;
        command.Parameters.Add(newValueParam);

        DbParameter nameToUpdateParam = command.CreateParameter();
        nameToUpdateParam.ParameterName = "@nameToUpdate";
        nameToUpdateParam.Value = nameToUpdate;
        command.Parameters.Add(nameToUpdateParam);

        await command.ExecuteNonQueryAsync();
        stopwatch.Stop();

        Console.WriteLine($"\nЧас оновлення {attributeName} для {nameToUpdate}: {stopwatch.Elapsed.TotalMilliseconds} мс");
        Console.ReadKey();
        Console.Clear();
    }

    private static async Task<bool> CheckIfExistsAsync(string name)
    {
        connection = dbProviderFactory.CreateConnection();
        connection.ConnectionString = $"Data Source={sqliteFile}";
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM fruits_and_vegetables WHERE name = @name";

        DbParameter nameParam = command.CreateParameter();
        nameParam.ParameterName = "@name";
        nameParam.Value = name;
        command.Parameters.Add(nameParam);

        var result = await command.ExecuteScalarAsync();
        int count = Convert.ToInt32(result);

        connection.Close();

        return count > 0;
    }

}
