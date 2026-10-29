using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _3dgame;

public class GameTextures
{

    public Texture2D enemyTexture1 ;
    public Texture2D crossHair ;
    public Texture2D pixel;
    
}

public class Game1 : Game {
    private GraphicsDeviceManager graphics;
    private GameObject ground;

    //MY STUFF
    private GameObject test;
    
    private readonly Matrix testWorld = Matrix.CreateScale(25f) * Matrix.CreateTranslation(60, 2.5f, 100);
    // the current room, picked in order by currentLevel -> LoadLevel()
    private GameObject level;
    private int currentLevel = 1;
    // content manager for the cur level
    private ContentManager levelContent;
    private readonly Random random = new Random();

    private GameTextures textures = new GameTextures() ;
    private List<Enemy> enemyList = new();

    // WHERE THE LEVEL SPAWNS
    private readonly Matrix levelMatrix = Matrix.CreateTranslation(0, 0, 0);
    private Camera gameCamera;
    private Player player;
    private readonly List<BoundingBox> colliders = new List<BoundingBox>();
    private KeyboardState currentKeyboardState = new KeyboardState();
    private MouseState currentMouseState = new MouseState();
    private MouseState previousMouseState = new MouseState();
    private bool mouseCentered = false;
    private int screenCenterX;
    private int screenCenterY;
    private float cameraYaw;
    private float cameraPitch;
    private double logTimer = 0d;
    private double totalMouseIdleTime = 0  ;
    private SpriteBatch spriteBatch;
    private SpriteFont debugFont; // DELETE LATER


    public Game1() {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        graphics.SynchronizeWithVerticalRetrace = true;

        graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        graphics.ToggleFullScreen();

        graphics.ApplyChanges();
        IsMouseVisible = false;
    }

    protected override void Initialize() {
        ground = new GameObject();

        spriteBatch = new SpriteBatch(GraphicsDevice);
    
        test = new GameObject();
        level = new GameObject();
        levelContent = new ContentManager(Services, Content.RootDirectory);

        gameCamera = new Camera();
        player = new Player();
        screenCenterX = GraphicsDevice.Viewport.Width/2;
        screenCenterY = GraphicsDevice.Viewport.Height/2;
        Mouse.SetPosition(screenCenterX, screenCenterY);
        previousMouseState = Mouse.GetState();

        textures.enemyTexture1 = Content.Load<Texture2D>("Models/enemy");


        //enemyList.Add(new Enemy(GraphicsDevice, textures.enemyTexture1 , new Vector3(50f, 0f, 75f), 9f, 11f));

        base.Initialize();
    }

    protected override void LoadContent() {
        ChangeRasterizerState(FillMode.Solid);
        ground.Model = Content.Load<Model>("Models/ground");

        test.Model = Content.Load<Model>("Models/test");

        textures.pixel = Content.Load<Texture2D>("Models/White");
        debugFont = Content.Load<SpriteFont>("Fonts/Debug"); // DELETE LATER

        player.Model = Content.Load<Model>("Models/player");
        player.CalculateBoundingSphere();

        LoadLevel();
    }

//good practice bellow, not sure what it does LMAOOOO
    protected override void UnloadContent() {
        levelContent.Unload();
        base.UnloadContent();
    }

    private void LoadLevel() {        //LEVEL INITIALISED HERE
        levelContent.Unload();
        enemyList.Clear();

        switch(currentLevel)
        {
        case 1:
            level.Model = levelContent.Load<Model>("Models/level1");
            enemyList.Add(new Enemy(GraphicsDevice, textures.enemyTexture1, new Vector3(20f, 0f, 20f), 9f, 11f));

            break;
        default:
            currentLevel = 1;
            LoadLevel();
            return;
        }

        colliders.Clear();
        colliders.AddRange(ModelCollision.Build(test.Model, testWorld));
        colliders.AddRange(ModelCollision.Build(level.Model, levelMatrix, perMesh: true));


    }

    private void CompleteLevel() {
        Console.WriteLine("YOU DID IT");
        currentLevel++;
        LoadLevel();
        player.ResetToStart();
        cameraYaw = 0f;
        cameraPitch = 0f;
    }

    protected override void Update(GameTime gameTime) {
        if (!IsActive) {
            base.Update(gameTime);
            return;
        }

        
        currentKeyboardState = Keyboard.GetState();

        // MOUSE & CAMERA INTERACTIONS 

        currentMouseState = Mouse.GetState();

        float maxPitchNegative = MathHelper.ToRadians(-85f);
        float maxPitchPositive = MathHelper.ToRadians(85f);

        if (currentMouseState != previousMouseState)
        {
            float deltaX = currentMouseState.X - previousMouseState.X;
            float deltaY = currentMouseState.Y - previousMouseState.Y;
            
            cameraYaw -= deltaX * GameConstants.sensitivity;
            float pitchDelta = -deltaY * GameConstants.sensitivity;
            float newPitch = cameraPitch + pitchDelta;

            if (!((cameraPitch >= maxPitchPositive && pitchDelta > 0f) || (cameraPitch <= maxPitchNegative && pitchDelta < 0f))) {
                cameraPitch = newPitch;
            }
            
            cameraPitch = MathHelper.Clamp(cameraPitch, maxPitchNegative, maxPitchPositive);   
        
            totalMouseIdleTime = 0 ; 
            previousMouseState = currentMouseState;

            
            // check if mouse is near/at border, if yes, move it to senter
            if (currentMouseState.X >= graphics.GraphicsDevice.Viewport.Width - (50* 1920/graphics.GraphicsDevice.Viewport.Width) || 
                currentMouseState.Y >= graphics.GraphicsDevice.Viewport.Height - (35 * 1080/graphics.GraphicsDevice.Viewport.Height) || 
                currentMouseState.X == 0 || currentMouseState.Y == 0)
            {
                Mouse.SetPosition(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2);
                previousMouseState = Mouse.GetState();  
            }

        } else
        {
            previousMouseState = currentMouseState;
            
            totalMouseIdleTime += (float)gameTime.ElapsedGameTime.TotalSeconds ;
            if (totalMouseIdleTime >= 0.7 && Mouse.GetState().X != graphics.GraphicsDevice.Viewport.Width / 2 && Mouse.GetState().Y != graphics.GraphicsDevice.Viewport.Height / 2)
            {
                totalMouseIdleTime = 0 ; 
                Mouse.SetPosition(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2);
                previousMouseState = Mouse.GetState();
            }
        }

        player.Update(currentKeyboardState, cameraYaw, colliders);

        if (player.Position.Z <= GameConstants.LevelEndZ) {
            CompleteLevel();
        }

        gameCamera.Update(cameraYaw, cameraPitch, player.Position, graphics.GraphicsDevice.Viewport.AspectRatio);

        if (currentKeyboardState.IsKeyDown(Keys.Escape)) {
            Exit();
        }

        logTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (logTimer >= 3) {
            Console.WriteLine("posX: " + player.Position.X + " posY: " + player.Position.Y + " posZ: " + player.Position.Z);
            logTimer = 0;
        }
        
        base.Update(gameTime);
    }

    private void DrawTerrain(Model model) {
        foreach (ModelMesh mesh in model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                //effect.EnableDefaultLighting();

                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
                effect.SpecularColor = Vector3.Zero;

                effect.PreferPerPixelLighting = true;
                effect.World = Matrix.Identity;

                // use the matrices provided by the game camera
                effect.View = gameCamera.ViewMatrix;
                effect.Projection = gameCamera.ProjectionMatrix;
            }
            mesh.Draw();
        }
    }

    private void DrawTextures(Model model, Matrix world) {
        foreach (ModelMesh mesh in model.Meshes) {
            if (ModelCollision.IsCollider(mesh)) continue;   // col_ meshes are collision-only
            foreach (BasicEffect effect in mesh.Effects) {
                //effect.EnableDefaultLighting();

                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
                effect.SpecularColor = Vector3.Zero;

                effect.PreferPerPixelLighting = true;
                effect.TextureEnabled = true;
                effect.World = world;

                // use the matrices provided by the game camera
                effect.View = gameCamera.ViewMatrix;
                effect.Projection = gameCamera.ProjectionMatrix;
            }
            mesh.Draw();
        }
    }

    private void DrawHud()
    {
        int w = graphics.GraphicsDevice.Viewport.Width;
        int h = graphics.GraphicsDevice.Viewport.Height;

        // AlphaBlend matches textures from the Content Pipeline, which premultiplies alpha by default.
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                            DepthStencilState.None, RasterizerState.CullNone);


        // Crosshair
        const int len = 8, gap = 4, thick = 2;
        int cx = w / 2, cy = h / 2;
        spriteBatch.Draw(textures.pixel, new Rectangle(cx - gap - len, cy - thick / 2, len, thick), Color.White);
        spriteBatch.Draw(textures.pixel, new Rectangle(cx + gap, cy - thick / 2, len, thick), Color.White);
        spriteBatch.Draw(textures.pixel, new Rectangle(cx - thick / 2, cy - gap - len, thick, len), Color.White);
        spriteBatch.Draw(textures.pixel, new Rectangle(cx - thick / 2, cy + gap, thick, len), Color.White);

        string zText = "Z: " + player.Position.Z.ToString("0.0") + " / " + GameConstants.LevelEndZ; // DELETE LATER
        spriteBatch.DrawString(debugFont, zText, new Vector2(21, 21), Color.Black); // DELETE LATER (shadow)
        spriteBatch.DrawString(debugFont, zText, new Vector2(20, 20), Color.Yellow); // DELETE LATER

        spriteBatch.End();
    }
    
    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.WhiteSmoke);

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

        DrawTerrain(ground.Model);   // GROUND IS RENDERED HERE

        DrawTextures(test.Model, testWorld);
        DrawTextures(level.Model, levelMatrix);

        foreach (Enemy enemy in enemyList) {
            enemy.Draw(GraphicsDevice, gameCamera.ViewMatrix, gameCamera.ProjectionMatrix, player.Position);
        }

        // HUD last, so it draws on top of the 3D scene
        DrawHud();

        base.Draw(gameTime);
    }

    private RasterizerState ChangeRasterizerState(FillMode fillmode, CullMode cullMode = CullMode.None) {
        RasterizerState rasterizerState = new RasterizerState() { 
            FillMode = fillmode,
            CullMode = cullMode 
        };
        graphics.GraphicsDevice.RasterizerState = rasterizerState;
        return rasterizerState;
    }
}