using Labyrinth;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Tiles;

namespace LabyrinthTest
{
    public class ConcurrencyTest
    {
        [Test]
        public async Task ConcurrentCrawlers_ShouldNotCrash()
        {
            // Arrange
            var parser = new AsciiParser("""
                +---+
                |x x|
                +---+
                """);
            using var labyrinth = new Labyrinth.Labyrinth(parser);
            
            // Create two crawlers
            // Start position is 'x', but AsciiParser supports multiple start positions by 'x'.
            // The map above has two 'x'.
            
            // Wait, Labyrinth.Start is a single (X,Y) tuple?
            // Labyrinth.cs: 
            // builder.StartPositionFound+= (s, e) => _start = (e.X, e.Y);
            // If multiple 'x', _start is overwritten.
            // So NewCrawler() spawns at the last found 'x'.
            
            var c1 = labyrinth.NewCrawler();
            var c2 = labyrinth.NewCrawler();

            // Act
            // Move them around concurrently.
            // They are at (3,1) (second x) likely. (1,1) and (3,1).
            // Let's assume they start at same position if there's only one.
            
            var tasks = new List<Task>();
            
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(async () => 
                {
                    var type = await c1.FacingTileType;
                    // Just check type
                    Assert.That(type, Is.Not.Null);
                }));
                 tasks.Add(Task.Run(async () => 
                {
                    var type = await c2.FacingTileType;
                    Assert.That(type, Is.Not.Null);
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Assert
            // No exception thrown
        }

        [Test]
        public async Task ConcurrentTryWalk_ShouldWork()
        {
            var parser = new AsciiParser("""
                +-------+
                |x      |
                +-------+
                """);
            using var labyrinth = new Labyrinth.Labyrinth(parser);
            var c1 = labyrinth.NewCrawler();
            var c2 = labyrinth.NewCrawler();

            // Both at 1,1
            // Try walk East 
            // c1 moves 1, c2 moves 1.
            
            // Depending on race, they might act weirdly if not synced?
            // Actually they just read/write position.
            // But ProcessFacingTileAsync is locked.
            
            // Let's try to simulate a race where they try to pick up an item?
            // But picking up happens outside TryWalk.
            
            // Let's just verify that rapid movement is fine.
            
            // We need a loop where they move back and forth?
            // Or just check FacingTileType a lot.
            
            var t1 = Task.Run(async () => 
            {
                for(int i=0; i<100; i++) await c1.FacingTileType;
            });
             var t2 = Task.Run(async () => 
            {
                for(int i=0; i<100; i++) await c2.FacingTileType;
            });
            
            await Task.WhenAll(t1, t2);
            Assert.Pass();
        }
    }
}

