using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame_Game_Library;
using SharpDX.Direct3D9;

namespace Project2
{
    public class Game1 : Core
    {
        // Defines the slime sprite.
        private Sprite _slime;

        // Defines the bat sprite.
        private Sprite _bat;
        public Game1() : base("Tày Đặt Bom", 1280, 720, false)
        {

        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // TODO: use this.Content to load your game content here
            TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");
            // Create the slime sprite from the atlas.
            _slime = atlas.CreateSprite("slime");
            _slime.Scale = new Vector2(4.0f, 4.0f);

            // Create the bat sprite from the atlas.
            _bat = atlas.CreateSprite("bat");
            _bat.Scale = new Vector2(4.0f, 4.0f);

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            // Begin the sprite batch to prepare for rendering.
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Draw the slime sprite.
            _slime.Draw(SpriteBatch, Vector2.Zero);

            // Draw the bat sprite 10px to the right of the slime.
            _bat.Draw(SpriteBatch, new Vector2(_slime.Width + 10, 0));

            // Always end the sprite batch when finished.
            SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
