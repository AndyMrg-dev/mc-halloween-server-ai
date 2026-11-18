using System;
using System.IO;
using Microsoft.Data.Sqlite;

const int MinRep = -300;
const int MaxRep = 100;

// nosferatu_data.db is located one level above admin_cli
string dbPath = File.Exists("../nosferatu_data.db") ? "../nosferatu_data.db" : "nosferatu_data.db";

if (!File.Exists(dbPath))
{
    Console.WriteLine($"Database not found at: {Path.GetFullPath(dbPath)}");
    return;
}

string connStr = $"Data Source={dbPath}";

while (true)
{
    Console.WriteLine("\n=== NOSFERATU ENGINE - ADMIN CLI ===");
    Console.WriteLine("1. Show all players & reputation");
    Console.WriteLine("2. Show recent AI action logs");
    Console.WriteLine("3. Show death logs");
    Console.WriteLine("4. Adjust player reputation");
    Console.WriteLine("5. Reset player reputation to 0");
    Console.WriteLine("0. Exit");
    Console.Write("\nSelect option: ");

    string? choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            ShowPlayers(connStr);
            break;
        case "2":
            ShowActionLogs(connStr);
            break;
        case "3":
            ShowDeathLogs(connStr);
            break;
        case "4":
            AdjustReputation(connStr);
            break;
        case "5":
            ResetReputation(connStr);
            break;
        case "0":
            Console.WriteLine("Farewell, mortal.");
            return;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

static SqliteCommand CreateCommand(SqliteConnection conn, string sql)
{
    var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    return cmd;
}

static void ShowPlayers(string connStr)
{
    using var conn = new SqliteConnection(connStr);
    conn.Open();

    var cmd = CreateCommand(conn, @"
        SELECT player_name, reputation, deaths, interactions, last_seen
        FROM players
        ORDER BY reputation DESC, last_seen DESC");

    using var reader = cmd.ExecuteReader();
    Console.WriteLine("\n--- Players & Favor ---");

    bool any = false;
    while (reader.Read())
    {
        any = true;
        Console.WriteLine(
            $"{reader.GetString(0),-16} | Rep: {reader.GetInt32(1),4} | Interactions: {reader.GetInt32(3),3} | Deaths: {reader.GetInt32(2),2} | Last seen: {reader.GetString(4)}"
        );
    }
    if (!any) Console.WriteLine("No players recorded yet.");
}

static void ShowActionLogs(string connStr)
{
    using var conn = new SqliteConnection(connStr);
    conn.Open();

    var cmd = CreateCommand(conn, @"
        SELECT id, player_name, message, rep_change, new_rep, actions, created_at
        FROM action_logs
        ORDER BY id DESC
        LIMIT 15");

    using var reader = cmd.ExecuteReader();
    Console.WriteLine("\n--- Recent AI Action Logs (Last 15) ---");

    bool any = false;
    while (reader.Read())
    {
        any = true;
        int id = reader.GetInt32(0);
        string player = reader.GetString(1);
        string msg = reader.IsDBNull(2) ? "" : reader.GetString(2);
        int delta = reader.GetInt32(3);
        int newRep = reader.GetInt32(4);
        string actions = reader.IsDBNull(5) ? "none" : reader.GetString(5);
        string time = reader.GetString(6);

        Console.WriteLine($"[#{id}] {time} | {player} (Rep: {newRep}, {delta:+0;-0;0}) | Action: {actions}");
        Console.WriteLine($"      Chat: \"{msg}\"");
    }
    if (!any) Console.WriteLine("No action logs found.");
}

static void ShowDeathLogs(string connStr)
{
    using var conn = new SqliteConnection(connStr);
    conn.Open();

    var cmd = CreateCommand(conn, @"
        SELECT id, player_name, cause, rep_before, created_at
        FROM death_logs
        ORDER BY id DESC
        LIMIT 15");

    using var reader = cmd.ExecuteReader();
    Console.WriteLine("\n--- Death Logs (Last 15) ---");

    bool any = false;
    while (reader.Read())
    {
        any = true;
        Console.WriteLine(
            $"[#{reader.GetInt32(0)}] {reader.GetString(4)} | {reader.GetString(1)} ({reader.GetString(2)}) | Lost Rep: {reader.GetInt32(3)}"
        );
    }
    if (!any) Console.WriteLine("No deaths recorded.");
}

static void AdjustReputation(string connStr)
{
    Console.Write("Player name: ");
    string player = (Console.ReadLine() ?? "").Trim();
    if (string.IsNullOrEmpty(player))
    {
        Console.WriteLine("Invalid player name.");
        return;
    }

    Console.Write("Reputation delta (e.g. 25 or -50): ");
    if (!int.TryParse(Console.ReadLine(), out int delta))
    {
        Console.WriteLine("Invalid number.");
        return;
    }

    using var conn = new SqliteConnection(connStr);
    conn.Open();

    var selectCmd = CreateCommand(conn, "SELECT reputation FROM players WHERE player_name = $player");
    selectCmd.Parameters.AddWithValue("$player", player);

    object? result = selectCmd.ExecuteScalar();
    if (result is null)
    {
        Console.WriteLine($"Player '{player}' not found in database.");
        return;
    }

    int currentRep = Convert.ToInt32(result);
    int updatedRep = Math.Clamp(currentRep + delta, MinRep, MaxRep);

    var updateCmd = CreateCommand(conn, "UPDATE players SET reputation = $rep WHERE player_name = $player");
    updateCmd.Parameters.AddWithValue("$rep", updatedRep);
    updateCmd.Parameters.AddWithValue("$player", player);
    updateCmd.ExecuteNonQuery();

    Console.WriteLine($"Updated {player}'s reputation: {currentRep} -> {updatedRep}");
}

static void ResetReputation(string connStr)
{
    Console.Write("Player name to reset: ");
    string player = (Console.ReadLine() ?? "").Trim();
    if (string.IsNullOrEmpty(player))
    {
        Console.WriteLine("Invalid player name.");
        return;
    }

    using var conn = new SqliteConnection(connStr);
    conn.Open();

    var cmd = CreateCommand(conn, "UPDATE players SET reputation = 0 WHERE player_name = $player");
    cmd.Parameters.AddWithValue("$player", player);

    int rows = cmd.ExecuteNonQuery();
    Console.WriteLine(rows > 0
        ? $"Reputation for '{player}' reset to 0."
        : $"Player '{player}' not found.");
}