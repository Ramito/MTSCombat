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

        private const int kArenaWidth = 1280;
        private const int kArenaHeight = 720;
        private GraphicsDeviceManager mGraphics;
        private MTSCombatRenderer mVehicleRenderer;
        private MTSCombatGame mMTSGame;
        
        public Game1()
        {
            mGraphics = new GraphicsDeviceManager(this);
            MatchCurrentResolution(mGraphics);
            Content.RootDirectory = "Content";
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

            PrimitiveRenderer primitiveRenderer = new PrimitiveRenderer();
            primitiveRenderer.Setup(mGraphics.GraphicsDevice, kArenaWidth, kArenaHeight);
            mVehicleRenderer = new MTSCombatRenderer(primitiveRenderer);

            mMTSGame = new MTSCombatGame(2, kArenaWidth, kArenaHeight);
            AsteroidsControlData data = new AsteroidsControlData(60f, 90f, 3f);
            AsteroidsControls asteroidsControls = new AsteroidsControls(data);
            VehicleState state = new VehicleState();
            state.SetControllerID(0);
            state.SetState(5f, new DynamicTransform2(new Vector2(kArenaWidth / 2, kArenaHeight / 2), new Orientation2(0f)), asteroidsControls);
            GunMount gunMount = new GunMount(new GunData(0.3f, 720f), new Vector2[] { 2f * Vector2.UnitX, 3f * Vector2.UnitY, -3f * Vector2.UnitY });
            mMTSGame.AddVehicle(state, gunMount);

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

            mVehicleRenderer.RenderSimState(mMTSGame.ActiveState);

            base.Draw(gameTime);
        }
    }
}
