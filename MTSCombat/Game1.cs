using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MTSCombat.Render;
using MTSCombat.Simulation;

namespace MTSCombat
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {

        private const int kArenaWidth = 800;
        private const int kArenaHeight = 600;
        private GraphicsDeviceManager mGraphics;
        private PrimitiveRenderer mPrimitiveRenderer;
        private VehicleRenderer mVehicleRenderer;
        private MTSCombatGame mMTSGame;
        
        public Game1()
        {
            mGraphics = new GraphicsDeviceManager(this);
            MatchCurrentResolution(mGraphics);
            Content.RootDirectory = "Content";
            mPrimitiveRenderer = new PrimitiveRenderer();
            mVehicleRenderer = new VehicleRenderer();
        }

        private static void MatchCurrentResolution(GraphicsDeviceManager graphics)
        {
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            //TODO: Toggling full screen makes debugging a pain
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.None;
            mGraphics.GraphicsDevice.RasterizerState = rs;

            mPrimitiveRenderer.Setup(mGraphics.GraphicsDevice, kArenaWidth, kArenaHeight);

            mMTSGame = new MTSCombatGame(2);
            AsteroidsControlData data = new AsteroidsControlData(20f, 30f, 2f);
            AsteroidsControls asteroidsControls = new AsteroidsControls(data);
            VehicleState state = new VehicleState();
            state.SetControllerID(0);
            state.SetState(5f, new DynamicTransform2(new Vector2(kArenaWidth / 2, kArenaHeight / 2), new Orientation2(0f)), asteroidsControls);
            mMTSGame.AddVehicle(state);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            mMTSGame.Tick((float)TargetElapsedTime.TotalSeconds);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            mVehicleRenderer.RenderVehicles(mMTSGame.ActiveState.Vehicles, mPrimitiveRenderer);
            mPrimitiveRenderer.Render();

            base.Draw(gameTime);
        }
    }
}
