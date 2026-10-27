using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
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
    private GameObject corridor;
    // corridor runs along Z, entrance 5 units in front of the player's start (30, 2, 125)

    private GameTextures textures = new GameTextures() ;
    private List<Enemy> enemyList = new();

    private readonly Matrix levelMatrix = Matrix.CreateTranslation(30, 0, 100);
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
        corridor = new GameObject();

        gameCamera = new Camera();
        player = new Player();
        screenCenterX = GraphicsDevice.Viewport.Width/2;
        screenCenterY = GraphicsDevice.Viewport.Height/2;
        Mouse.SetPosition(screenCenterX, screenCenterY);
        previousMouseState = Mouse.GetState();

        textures.enemyTexture1 = Content.Load<Texture2D>("Models/enemy");

        enemyList.Add(new Enemy(GraphicsDevice, textures.enemyTexture1 , new Vector3(50f, 0f, 75f), 9f, 11f));

        base.Initialize();
    }

    protected override void LoadContent() {
        ChangeRasterizerState(FillMode.Solid);
        ground.Model = Content.Load<Model>("Models/ground");

        test.Model = Content.Load<Model>("Models/test");

        textures.pixel = Content.Load<Texture2D>("Models/White");

        colliders.AddRange(ModelCollision.Build(test.Model, testWorld));
        corridor.Model = Content.Load<Model>("Models/corridor");
        colliders.AddRange(ModelCollision.Build(corridor.Model, levelMatrix, perMesh: true));
        player.Model = Content.Load<Model>("Models/player");
        player.CalculateBoundingSphere();
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

        gameCamera.Update(cameraYaw, cameraPitch, player.Position, graphics.GraphicsDevice.Viewport.AspectRatio);


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
                effect.EnableDefaultLighting();
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
                effect.EnableDefaultLighting();
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

        // NonPremultiplied matches PNGs loaded with Texture2D.FromStream (soft edges look right).
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp,
                            DepthStencilState.None, RasterizerState.CullNone);


        // Crosshair
        const int len = 8, gap = 4, thick = 2;
        int cx = w / 2, cy = h / 2;
        spriteBatch.Draw(textures.pixel, new Rectangle(cx - gap - len, cy - thick / 2, len, thick), Color.White);
        spriteBatch.Draw(textures.pixel, new Rectangle(cx + gap, cy - thick / 2, len, thick), Color.White);
        spriteBatch.Draw(textures.pixel, new Rectangle(cx - thick / 2, cy - gap - len, thick, len), Color.White);
        spriteBatch.Draw(textures.pixel, new Rectangle(cx - thick / 2, cy + gap, thick, len), Color.White);

        spriteBatch.End();
    }
    
    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.WhiteSmoke);
        DrawTerrain(ground.Model);

        DrawTextures(test.Model, testWorld);
        DrawTextures(corridor.Model, levelMatrix);

        foreach (Enemy enemy in enemyList)
            //enemy.Draw(GraphicsDevice, gameCamera.ViewMatrix, gameCamera.ProjectionMatrix, player.Position);

        //DrawHud();

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