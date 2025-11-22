using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RelicEscape
{
    // Enums
    public enum TileType
    {
        Grass,
        Tree,
        Water,
        Stone,
        Chest,
        Exit
    }

    public enum EntityState
    {
        Idle,
        Walking,
        Attacking,
        Dead
    }

    public enum EnemyType
    {
        Snake,
        Spider
    }

    // Player Class
    public class Player
    {
        public Vector2 Position;
        public int Health;
        public int MaxHealth;
        public List<string> Inventory;
        public EntityState State;
        public float Speed;
        public Rectangle Bounds;
        public bool IsAttacking;
        public float AttackCooldown;
        public int AttackRange;

        public Player(Vector2 startPos)
        {
            Position = startPos;
            Health = 100;
            MaxHealth = 100;
            Inventory = new List<string>();
            State = EntityState.Idle;
            Speed = 150f;
            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
            IsAttacking = false;
            AttackCooldown = 0f;
            AttackRange = 50;
        }

        public void Update(float deltaTime, KeyboardState keyState, KeyboardState prevKeyState)
        {
            // Update attack cooldown
            if (AttackCooldown > 0)
                AttackCooldown -= deltaTime;

            // Movement
            Vector2 movement = Vector2.Zero;

            if (keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.Up))
                movement.Y -= 1;
            if (keyState.IsKeyDown(Keys.S) || keyState.IsKeyDown(Keys.Down))
                movement.Y += 1;
            if (keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.Left))
                movement.X -= 1;
            if (keyState.IsKeyDown(Keys.D) || keyState.IsKeyDown(Keys.Right))
                movement.X += 1;

            if (movement != Vector2.Zero)
            {
                movement.Normalize();
                Position += movement * Speed * deltaTime;
                State = EntityState.Walking;
            }
            else
            {
                State = EntityState.Idle;
            }

            // Attack
            if (keyState.IsKeyDown(Keys.Space) && prevKeyState.IsKeyUp(Keys.Space) && AttackCooldown <= 0)
            {
                IsAttacking = true;
                AttackCooldown = 0.5f;
            }
            else if (AttackCooldown <= 0)
            {
                IsAttacking = false;
            }

            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health < 0) Health = 0;
        }

        public void AddToInventory(string item)
        {
            Inventory.Add(item);
        }

        public int GetKeyCount()
        {
            return Inventory.Count(item => item == "Key");
        }
    }

    // Enemy Class
    public class Enemy
    {
        public Vector2 Position;
        public EnemyType Type;
        public int Health;
        public EntityState State;
        public float Speed;
        public Rectangle Bounds;
        public float DetectionRange;
        public float AttackRange;
        public float AttackCooldown;
        public bool DropsKey;
        public Vector2 WanderTarget;
        public float WanderTimer;

        public Enemy(Vector2 startPos, EnemyType type, bool dropsKey = false)
        {
            Position = startPos;
            Type = type;
            Health = type == EnemyType.Snake ? 30 : 25;
            State = EntityState.Idle;
            Speed = type == EnemyType.Snake ? 100f : 80f;
            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
            DetectionRange = 200f;
            AttackRange = 35f;
            AttackCooldown = 0f;
            DropsKey = dropsKey;
            WanderTarget = startPos;
            WanderTimer = 0f;
        }

        public void Update(float deltaTime, Player player, Random random)
        {
            if (State == EntityState.Dead) return;

            float distanceToPlayer = Vector2.Distance(Position, player.Position);

            // Update attack cooldown
            if (AttackCooldown > 0)
                AttackCooldown -= deltaTime;

            // Chase player if in range
            if (distanceToPlayer < DetectionRange)
            {
                Vector2 direction = player.Position - Position;
                if (direction != Vector2.Zero)
                {
                    direction.Normalize();
                    Position += direction * Speed * deltaTime;
                    State = EntityState.Walking;
                }

                // Attack if in range
                if (distanceToPlayer < AttackRange && AttackCooldown <= 0)
                {
                    player.TakeDamage(5);
                    AttackCooldown = 1.5f;
                }
            }
            else
            {
                // Wander randomly
                WanderTimer -= deltaTime;
                if (WanderTimer <= 0)
                {
                    WanderTarget = Position + new Vector2(
                        random.Next(-100, 100),
                        random.Next(-100, 100)
                    );
                    WanderTimer = random.Next(2, 5);
                }

                Vector2 direction = WanderTarget - Position;
                if (direction.Length() > 5f)
                {
                    direction.Normalize();
                    Position += direction * Speed * 0.5f * deltaTime;
                    State = EntityState.Walking;
                }
                else
                {
                    State = EntityState.Idle;
                }
            }

            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                State = EntityState.Dead;
            }
        }
    }

    // Interactive Object Class
    public class InteractiveObject
    {
        public Vector2 Position;
        public Rectangle Bounds;
        public string Type;
        public bool IsActivated;

        public InteractiveObject(Vector2 pos, string type, int width = 48, int height = 48)
        {
            Position = pos;
            Type = type;
            Bounds = new Rectangle((int)pos.X, (int)pos.Y, width, height);
            IsActivated = false;
        }
    }

    // Main Game Class
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Game state
        private Player player;
        private List<Enemy> enemies;
        private List<InteractiveObject> objects;
        private TileType[,] map;
        private int mapWidth = 16;
        private int mapHeight = 16;
        private int tileSize = 64;
        private Random random;

        // Game logic
        private int snakesKilled = 0;
        private int spidersKilled = 0;
        private bool hasRelicBlade = false;
        private float enemyRespawnTimer = 0f;
        private float enemyRespawnDelay = 30f;
        private KeyboardState prevKeyState;

        // Rendering
        private Texture2D pixelTexture;
        private SpriteFont font;
        private bool showVictoryMessage = false;
        private string victoryMessage = "";

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
        }

        protected override void Initialize()
        {
            random = new Random();
            InitializeMap();
            InitializePlayer();
            InitializeEnemies();
            InitializeObjects();

            base.Initialize();
        }

        private void InitializeMap()
        {
            map = new TileType[mapWidth, mapHeight];

            // Fill with grass
            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                    map[x, y] = TileType.Grass;

            // Add trees around borders
            for (int x = 0; x < mapWidth; x++)
            {
                map[x, 0] = TileType.Tree;
                map[x, mapHeight - 1] = TileType.Tree;
            }
            for (int y = 0; y < mapHeight; y++)
            {
                map[0, y] = TileType.Tree;
                map[mapWidth - 1, y] = TileType.Tree;
            }

            // Add river (horizontal) at row 10
            for (int x = 3; x < 13; x++)
            {
                map[x, 10] = TileType.Water;
            }

            // Add some scattered trees
            map[3, 3] = TileType.Tree;
            map[5, 4] = TileType.Tree;
            map[10, 3] = TileType.Tree;
            map[12, 5] = TileType.Tree;
            map[4, 7] = TileType.Tree;
            map[11, 8] = TileType.Tree;

            // Chest location (upper right area past the river)
            map[13, 13] = TileType.Chest;

            // Exit gate (bottom right)
            map[14, 14] = TileType.Exit;
        }

        private void InitializePlayer()
        {
            player = new Player(new Vector2(tileSize * 2, tileSize * 2));
            player.AddToInventory("Combat Knife");
        }

        private void InitializeEnemies()
        {
            enemies = new List<Enemy>();

            // 3 Snakes
            enemies.Add(new Enemy(new Vector2(tileSize * 4, tileSize * 5), EnemyType.Snake, true));
            enemies.Add(new Enemy(new Vector2(tileSize * 8, tileSize * 4), EnemyType.Snake, true));
            enemies.Add(new Enemy(new Vector2(tileSize * 6, tileSize * 8), EnemyType.Snake, true));

            // 3 Spiders
            enemies.Add(new Enemy(new Vector2(tileSize * 10, tileSize * 6), EnemyType.Spider, true));
            enemies.Add(new Enemy(new Vector2(tileSize * 5, tileSize * 12), EnemyType.Spider, true));
            enemies.Add(new Enemy(new Vector2(tileSize * 11, tileSize * 12), EnemyType.Spider, true));
        }

        private void InitializeObjects()
        {
            objects = new List<InteractiveObject>();

            // Giant vine that can be pushed to cross river
            objects.Add(new InteractiveObject(
                new Vector2(tileSize * 2, tileSize * 9),
                "Vine",
                64, 64
            ));

            // Ancient chest
            objects.Add(new InteractiveObject(
                new Vector2(tileSize * 13, tileSize * 13),
                "Chest",
                64, 64
            ));
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create a 1x1 white pixel texture for drawing rectangles
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // Load font (you'll need to add a SpriteFont to your Content project)
            // For now, we'll handle if it's missing
            try
            {
                font = Content.Load<SpriteFont>("Font");
            }
            catch
            {
                // Font will be null, we'll handle text rendering differently
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyState = Keyboard.GetState();

            // Update player
            player.Update(deltaTime, keyState, prevKeyState);

            // Keep player in bounds
            player.Position.X = MathHelper.Clamp(player.Position.X, tileSize, (mapWidth - 2) * tileSize);
            player.Position.Y = MathHelper.Clamp(player.Position.Y, tileSize, (mapHeight - 2) * tileSize);

            // Check collision with tiles
            CheckTileCollisions();

            // Update enemies
            foreach (var enemy in enemies)
            {
                enemy.Update(deltaTime, player, random);
            }

            // Check player attacks on enemies
            if (player.IsAttacking)
            {
                CheckPlayerAttack();
            }

            // Check interactions with objects
            CheckObjectInteractions(keyState, prevKeyState);

            // Enemy respawn
            enemyRespawnTimer += deltaTime;
            if (enemyRespawnTimer >= enemyRespawnDelay)
            {
                RespawnEnemies();
                enemyRespawnTimer = 0f;
            }

            // Check victory condition
            if (hasRelicBlade)
            {
                CheckExitReached();
            }

            prevKeyState = keyState;
            base.Update(gameTime);
        }

        private void CheckTileCollisions()
        {
            int playerTileX = (int)(player.Position.X / tileSize);
            int playerTileY = (int)(player.Position.Y / tileSize);

            // Check surrounding tiles
            for (int x = playerTileX - 1; x <= playerTileX + 1; x++)
            {
                for (int y = playerTileY - 1; y <= playerTileY + 1; y++)
                {
                    if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
                        continue;

                    TileType tile = map[x, y];
                    Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                    if ((tile == TileType.Tree || tile == TileType.Water) &&
                        player.Bounds.Intersects(tileRect))
                    {
                        // Simple collision resolution
                        Vector2 tileCenterVec = new Vector2(x * tileSize + tileSize / 2, y * tileSize + tileSize / 2);
                        Vector2 direction = player.Position - tileCenterVec;
                        if (direction != Vector2.Zero)
                        {
                            direction.Normalize();
                            player.Position += direction * 2f;
                        }
                    }
                }
            }
        }

        private void CheckPlayerAttack()
        {
            foreach (var enemy in enemies)
            {
                if (enemy.State == EntityState.Dead) continue;

                float distance = Vector2.Distance(player.Position, enemy.Position);
                if (distance < player.AttackRange)
                {
                    enemy.TakeDamage(50);

                    if (enemy.State == EntityState.Dead && enemy.DropsKey)
                    {
                        // Alternate key drops: Snake -> Spider -> Snake
                        bool shouldDropKey = false;

                        if (enemy.Type == EnemyType.Snake && snakesKilled == 0 && player.GetKeyCount() == 0)
                        {
                            shouldDropKey = true;
                            snakesKilled++;
                        }
                        else if (enemy.Type == EnemyType.Spider && spidersKilled == 0 && player.GetKeyCount() == 1)
                        {
                            shouldDropKey = true;
                            spidersKilled++;
                        }
                        else if (enemy.Type == EnemyType.Snake && snakesKilled == 1 && player.GetKeyCount() == 2)
                        {
                            shouldDropKey = true;
                            snakesKilled++;
                        }

                        if (shouldDropKey)
                        {
                            player.AddToInventory("Key");
                            enemy.DropsKey = false;
                        }
                    }
                }
            }
        }

        private void CheckObjectInteractions(KeyboardState keyState, KeyboardState prevKeyState)
        {
            bool ePressed = keyState.IsKeyDown(Keys.E) && prevKeyState.IsKeyUp(Keys.E);

            foreach (var obj in objects)
            {
                float distance = Vector2.Distance(player.Position, obj.Position);

                if (obj.Type == "Vine" && !obj.IsActivated && distance < 60f && ePressed)
                {
                    // Push vine to create bridge
                    obj.Position.Y = tileSize * 10 - 16;
                    obj.IsActivated = true;

                    // Create bridge over river at x = 2
                    map[2, 10] = TileType.Stone;
                }

                if (obj.Type == "Chest" && !obj.IsActivated && distance < 60f && ePressed)
                {
                    if (player.GetKeyCount() >= 3)
                    {
                        // Open chest and get relic
                        player.AddToInventory("Spiritvine Blade");
                        hasRelicBlade = true;
                        obj.IsActivated = true;
                        victoryMessage = "You obtained the Spiritvine Blade! Head to the exit!";
                        showVictoryMessage = true;
                    }
                }
            }
        }

        private void RespawnEnemies()
        {
            int deadEnemies = enemies.Count(e => e.State == EntityState.Dead);
            if (deadEnemies == 0) return;

            // Respawn enemies at random positions
            foreach (var enemy in enemies.Where(e => e.State == EntityState.Dead))
            {
                int x = random.Next(2, mapWidth - 2);
                int y = random.Next(2, mapHeight - 2);

                // Make sure it's not water or tree
                while (map[x, y] == TileType.Water || map[x, y] == TileType.Tree)
                {
                    x = random.Next(2, mapWidth - 2);
                    y = random.Next(2, mapHeight - 2);
                }

                enemy.Position = new Vector2(x * tileSize, y * tileSize);
                enemy.Health = enemy.Type == EnemyType.Snake ? 30 : 25;
                enemy.State = EntityState.Idle;
            }
        }

        private void CheckExitReached()
        {
            int exitX = 14;
            int exitY = 14;
            Rectangle exitRect = new Rectangle(exitX * tileSize, exitY * tileSize, tileSize, tileSize);

            if (player.Bounds.Intersects(exitRect))
            {
                victoryMessage = "LEVEL COMPLETE! Press R to restart or ESC to exit";
                showVictoryMessage = true;

                // Here you would load the next level or show cutscene
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGreen);

            spriteBatch.Begin();

            // Draw map tiles
            DrawMap();

            // Draw objects
            DrawObjects();

            // Draw enemies
            DrawEnemies();

            // Draw player
            DrawPlayer();

            // Draw UI
            DrawUI();

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawMap()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    Color tileColor = Color.ForestGreen;

                    switch (map[x, y])
                    {
                        case TileType.Grass:
                            tileColor = Color.ForestGreen;
                            break;
                        case TileType.Tree:
                            tileColor = Color.SaddleBrown;
                            break;
                        case TileType.Water:
                            tileColor = Color.DodgerBlue;
                            break;
                        case TileType.Stone:
                            tileColor = Color.Gray;
                            break;
                        case TileType.Chest:
                            tileColor = Color.ForestGreen;
                            break;
                        case TileType.Exit:
                            tileColor = Color.Gold;
                            break;
                    }

                    spriteBatch.Draw(pixelTexture, tileRect, tileColor);

                    // Draw grid lines
                    DrawRectangle(tileRect, Color.Black * 0.2f, 1);
                }
            }
        }

        private void DrawObjects()
        {
            foreach (var obj in objects)
            {
                Color objColor = Color.White;

                if (obj.Type == "Vine")
                {
                    objColor = obj.IsActivated ? Color.Brown : Color.DarkGreen;
                    spriteBatch.Draw(pixelTexture, obj.Bounds, objColor);
                    DrawRectangle(obj.Bounds, Color.Black, 2);
                }
                else if (obj.Type == "Chest")
                {
                    objColor = obj.IsActivated ? Color.Yellow : Color.DarkGoldenrod;
                    spriteBatch.Draw(pixelTexture, obj.Bounds, objColor);
                    DrawRectangle(obj.Bounds, Color.Black, 2);
                }
            }
        }

        private void DrawEnemies()
        {
            foreach (var enemy in enemies)
            {
                if (enemy.State == EntityState.Dead) continue;

                Color enemyColor = enemy.Type == EnemyType.Snake ? Color.LimeGreen : Color.DarkViolet;
                Rectangle enemyRect = new Rectangle(
                    (int)enemy.Position.X,
                    (int)enemy.Position.Y,
                    32, 32
                );

                spriteBatch.Draw(pixelTexture, enemyRect, enemyColor);
                DrawRectangle(enemyRect, Color.Black, 2);

                // Health bar
                int healthBarWidth = 32;
                int currentHealthWidth = (int)(healthBarWidth * (enemy.Health / 30f));
                Rectangle healthBar = new Rectangle(
                    (int)enemy.Position.X,
                    (int)enemy.Position.Y - 8,
                    currentHealthWidth,
                    4
                );
                spriteBatch.Draw(pixelTexture, healthBar, Color.Red);
            }
        }

        private void DrawPlayer()
        {
            spriteBatch.Draw(pixelTexture, player.Bounds, Color.Blue);
            DrawRectangle(player.Bounds, Color.White, 2);

            // Attack indicator
            if (player.IsAttacking)
            {
                Rectangle attackRect = new Rectangle(
                    (int)player.Position.X - 10,
                    (int)player.Position.Y - 10,
                    52, 52
                );
                DrawRectangle(attackRect, Color.Red, 2);
            }
        }

        private void DrawUI()
        {
            // Background panel
            Rectangle uiPanel = new Rectangle(10, 10, 300, 150);
            spriteBatch.Draw(pixelTexture, uiPanel, Color.Black * 0.7f);
            DrawRectangle(uiPanel, Color.White, 2);

            // Health bar
            Rectangle healthBarBg = new Rectangle(20, 20, 200, 20);
            Rectangle healthBarFg = new Rectangle(20, 20, (int)(200 * (player.Health / 100f)), 20);
            spriteBatch.Draw(pixelTexture, healthBarBg, Color.DarkRed);
            spriteBatch.Draw(pixelTexture, healthBarFg, Color.Red);
            DrawRectangle(healthBarBg, Color.White, 2);

            if (font != null)
            {
                spriteBatch.DrawString(font, $"HP: {player.Health}/100", new Vector2(25, 22), Color.White);
                spriteBatch.DrawString(font, $"Keys: {player.GetKeyCount()}/3", new Vector2(25, 50), Color.White);
                spriteBatch.DrawString(font, "Inventory:", new Vector2(25, 80), Color.White);

                for (int i = 0; i < player.Inventory.Count; i++)
                {
                    spriteBatch.DrawString(font, $"- {player.Inventory[i]}", new Vector2(30, 100 + i * 20), Color.White);
                }

                // Controls
                spriteBatch.DrawString(font, "WASD: Move | Space: Attack | E: Interact",
                    new Vector2(10, graphics.PreferredBackBufferHeight - 30), Color.White);

                // Victory message
                if (showVictoryMessage)
                {
                    Vector2 msgSize = font.MeasureString(victoryMessage);
                    Vector2 msgPos = new Vector2(
                        graphics.PreferredBackBufferWidth / 2 - msgSize.X / 2,
                        graphics.PreferredBackBufferHeight / 2 - msgSize.Y / 2
                    );
                    Rectangle msgBg = new Rectangle(
                        (int)msgPos.X - 20,
                        (int)msgPos.Y - 20,
                        (int)msgSize.X + 40,
                        (int)msgSize.Y + 40
                    );
                    spriteBatch.Draw(pixelTexture, msgBg, Color.Black * 0.9f);
                    spriteBatch.DrawString(font, victoryMessage, msgPos, Color.Yellow);
                }
            }
        }

        private void DrawRectangle(Rectangle rect, Color color, int lineWidth)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, lineWidth), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - lineWidth, rect.Width, lineWidth), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, lineWidth, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - lineWidth, rect.Y, lineWidth, rect.Height), color);
        }
    }
}
