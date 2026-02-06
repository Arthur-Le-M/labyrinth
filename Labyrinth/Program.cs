using Labyrinth;
using Labyrinth.ApiClient;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using Labyrinth.Sys;
using Labyrinth.Exploration;
using Labyrinth.Exploration.Strategies;
using Labyrinth.Exploration.Strategies.Implementations;
using Labyrinth.Map;
using Dto=ApiTypes;
using System.Text.Json;

// Parse strategy from command line (--strategy=dfs, --strategy=random)
string strategyName = "random";
foreach (var arg in args)
{
    if (arg.StartsWith("--strategy=", StringComparison.OrdinalIgnoreCase))
    {
        strategyName = arg.Substring("--strategy=".Length).ToLower();
    }
}

char DirToChar(Direction dir) =>
    "^<v>"[dir.DeltaX * dir.DeltaX + dir.DeltaX + dir.DeltaY + 1];

var TileToChar = new Dictionary<Type, char>
{
    [typeof(Room   )] = ' ',
    [typeof(Wall   )] = '#',
    [typeof(Door   )] = '/'
};

void DrawExplorerForRand(object? sender, CrawlingEventArgs e)
{
    var crawler = ((RandExplorer)sender!).Crawler;
    var facingTileType = crawler.FacingTileType.Result;

    UpdateVisibleMap(e.X, e.Y, e.Direction, facingTileType);
    DrawSharedMapCells(e.X, e.Y, e.Direction, facingTileType);
    DrawFullMapCrawler(e.X, e.Y, e.Direction);

    SafeSetCursorPosition(0, 0);
    if (crawler is ClientCrawler cc)
    {
        Console.WriteLine($"Bag : { cc.Bag.ItemTypes.Count() } item(s)");
    }
    Thread.Sleep(100);
}

void DrawExplorerForStrategy(object? sender, CrawlingEventArgs e)
{
    var crawlerObj = ((StrategyExplorer)sender!).Crawler;
    var facingTileType = crawlerObj.FacingTileType.Result;

    UpdateVisibleMap(e.X, e.Y, e.Direction, facingTileType);
    DrawSharedMapCells(e.X, e.Y, e.Direction, facingTileType);
    DrawFullMapCrawler(e.X, e.Y, e.Direction);

    SafeSetCursorPosition(0, 0);
    Thread.Sleep(100);
}

Labyrinth.Labyrinth labyrinth;
ICrawler crawler;
Inventory? bag = null;
ContestSession? contest = null;

if (args.Length < 2 || args[0].StartsWith("--"))
{
    Console.WriteLine(
        "Command line usage: https://apiserver.example appKeyGuid [settings.json] [--strategy=random|dfs]"
    );
    Console.WriteLine($"Using strategy: {strategyName}");
    labyrinth = new Labyrinth.Labyrinth(new AsciiParser("""
        +--+--------+
        |  /        |
        |  +--+--+  |
        |     |k    |
        +--+  |  +--+
           |k  x    |
        +  +-------/|
        |           |
        +-----------+
        """));
    crawler = labyrinth.NewCrawler();
}
else
{
    Dto.Settings? settings = null;

    if (args.Length > 2 && !args[2].StartsWith("--"))
    {
        settings = JsonSerializer.Deserialize<Dto.Settings>(File.ReadAllText(args[2]));
    }
    contest = await ContestSession.Open(new Uri(args[0]), Guid.Parse(args[1]), settings);
    labyrinth = new (contest.Builder);
    crawler = await contest.NewCrawler();
    bag = contest.Bags.First();
}

// Create shared map for strategy-based exploration
var sharedMap = new SharedMap();

// Create exploration strategy based on CLI argument
IExplorationStrategy CreateStrategy(string name) => name switch
{
    "dfs" => new DfsStrategy(),
    "random" or _ => new RandomStrategy(new BasicEnumRandomizer<RandExplorer.Actions>())
};

var strategy = CreateStrategy(strategyName);

Console.WriteLine($"Exploration strategy: {strategy.Name}");

var prevX = crawler.X;
var prevY = crawler.Y;

var fullMap = BuildFullMap(labyrinth);
var mapWidth = fullMap.GetLength(0);
var mapHeight = fullMap.GetLength(1);
var visibleMap = CreateUnknownMap(mapWidth, mapHeight);

var fullMapTitleY = 1;
var fullMapOffsetY = fullMapTitleY + 1;
var sharedMapTitleY = fullMapOffsetY + mapHeight + 1;
var sharedMapOffsetY = sharedMapTitleY + 1;
var requiredBufferHeight = sharedMapOffsetY + mapHeight + 1;
var requiredBufferWidth = mapWidth + 1;
var canRenderSharedMap = EnsureConsoleBuffer(requiredBufferWidth, requiredBufferHeight);
var consoleLock = new object();

Console.Clear();
SafeSetCursorPosition(0, fullMapTitleY);
Console.WriteLine("Full map");
SafeSetCursorPosition(0, fullMapOffsetY);
DrawMap(fullMap);
if (canRenderSharedMap)
{
    SafeSetCursorPosition(0, sharedMapTitleY);
    Console.WriteLine("Shared map");
    SafeSetCursorPosition(0, sharedMapOffsetY);
    DrawMap(visibleMap);
}

// Use StrategyExplorer for DFS, RandExplorer for random (with visualization)
if (strategyName == "random")
{
    var explorer = new RandExplorer(
        crawler, 
        new BasicEnumRandomizer<RandExplorer.Actions>()
    );

    explorer.DirectionChanged += DrawExplorerForRand;
    explorer.PositionChanged  += (s, e) =>
    {
        try
        {
            RestorePreviousCellSingle();
            DrawExplorerForRand(s, e);
            (prevX, prevY) = (e.X, e.Y);
        }
        catch (ArgumentOutOfRangeException) { /* Ignore console bounds errors */ }
    };

    await explorer.GetOut(3000, bag);
}
else
{
    var strategyExplorer = new StrategyExplorer(crawler, strategy, sharedMap, 3000);
    
    strategyExplorer.DirectionChanged += DrawExplorerForStrategy;
    strategyExplorer.PositionChanged  += (s, e) =>
    {
        try
        {
            RestorePreviousCellSingle();
            DrawExplorerForStrategy(s, e);
            (prevX, prevY) = (e.X, e.Y);
        }
        catch (ArgumentOutOfRangeException) { /* Ignore console bounds errors */ }
    };
    
    await strategyExplorer.ExploreAsync();
}

if (contest is not null)
{
    await contest.Close();
}

return;

char[,] BuildFullMap(Labyrinth.Labyrinth lab)
{
    var lines = lab.ToString()
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    var height = lines.Length;
    var width = lines.Length == 0 ? 0 : lines.Max(l => l.Length);
    var map = new char[width, height];

    for (int y = 0; y < height; y++)
    {
        var line = lines[y];
        for (int x = 0; x < width; x++)
        {
            map[x, y] = x < line.Length ? line[x] : '?';
        }
    }

    return map;
}

char[,] CreateUnknownMap(int width, int height)
{
    var map = new char[width, height];
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            map[x, y] = '?';
        }
    }
    return map;
}

void DrawMap(char[,] map)
{
    for (int y = 0; y < map.GetLength(1); y++)
    {
        for (int x = 0; x < map.GetLength(0); x++)
        {
            Console.Write(map[x, y]);
        }
        Console.WriteLine();
    }
}

void UpdateVisibleMap(int x, int y, Direction dir, Type facingTileType)
{
    if (IsInBounds(x, y))
    {
        visibleMap[x, y] = ' ';
    }

    var facingX = x + dir.DeltaX;
    var facingY = y + dir.DeltaY;
    if (facingTileType != typeof(Outside) && IsInBounds(facingX, facingY))
    {
        visibleMap[facingX, facingY] = GetCharForTileType(facingTileType);
    }
}

void DrawSharedMapCells(int x, int y, Direction dir, Type facingTileType)
{
    if (!canRenderSharedMap)
    {
        return;
    }
    var facingX = x + dir.DeltaX;
    var facingY = y + dir.DeltaY;

    if (facingTileType != typeof(Outside) && IsInBounds(facingX, facingY))
    {
        SafeSetCursorPosition(facingX, facingY + sharedMapOffsetY);
        Console.Write(visibleMap[facingX, facingY]);
    }

    if (IsInBounds(x, y))
    {
        SafeSetCursorPosition(x, y + sharedMapOffsetY);
        Console.Write(DirToChar(dir));
    }
}

void DrawFullMapCrawler(int x, int y, Direction dir)
{
    if (IsInBounds(x, y))
    {
        SafeSetCursorPosition(x, y + fullMapOffsetY);
        Console.Write(DirToChar(dir));
    }
}

void RestorePreviousCellSingle()
{
    if (prevX >= 0 && prevY >= 0)
    {
        if (IsInBounds(prevX, prevY))
        {
            SafeSetCursorPosition(prevX, prevY + fullMapOffsetY);
            Console.Write(fullMap[prevX, prevY]);
            if (canRenderSharedMap)
            {
                SafeSetCursorPosition(prevX, prevY + sharedMapOffsetY);
                Console.Write(visibleMap[prevX, prevY]);
            }
        }
    }
}

void SafeSetCursorPosition(int x, int y)
{
    if (x < 0 || y < 0)
    {
        return;
    }

    if (x >= Console.BufferWidth || y >= Console.BufferHeight)
    {
        return;
    }

    Console.SetCursorPosition(x, y);
}

bool IsInBounds(int x, int y) =>
    x >= 0 && y >= 0 && x < visibleMap.GetLength(0) && y < visibleMap.GetLength(1);

char GetCharForTileType(Type tileType) =>
    TileToChar.TryGetValue(tileType, out var ch) ? ch : '?';

bool EnsureConsoleBuffer(int width, int height)
{
    try
    {
        if (Console.BufferWidth < width)
        {
            Console.BufferWidth = width;
        }
        if (Console.BufferHeight < height)
        {
            Console.BufferHeight = height;
        }
        return Console.BufferHeight >= height && Console.BufferWidth >= width;
    }
    catch (ArgumentOutOfRangeException)
    {
        return false;
    }
    catch (IOException)
    {
        return false;
    }
}
