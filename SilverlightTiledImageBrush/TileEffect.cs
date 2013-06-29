#define MY_DEP
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Linq;

namespace TileShaderEffect
{
    public class TileEffect : ShaderEffect
    {
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(TileEffect), 0);
        public Point TileCount
        {
            get
            {
                return ((Point)(this.GetValue(TileCountProperty)));
            }
            set
            {
                this.SetValue(TileCountProperty, value);
            }
        }

        #region TileCount Dependency Property

        /// <summary> 
        /// Identifies the TileCount dependency property. This enables animation, styling, binding, etc...
        /// </summary> 
        public static readonly DependencyProperty TileCountProperty =
            DependencyProperty.Register("TileCount",
                                        typeof(Point),
                                        typeof(TileEffect),
                                        new PropertyMetadata(new Point(0, 0), OnTileCountPropertyChanged));

        /// <summary>
        /// TileCount changed handler. 
        /// </summary>
        /// <param name="d">TileEffect that changed its TileCount.</param>
        /// <param name="e">DependencyPropertyChangedEventArgs.</param> 
        private static void OnTileCountPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PixelShaderConstantCallback(0)(d, e);
            var source = d as TileEffect;
            if (source != null)
            {
                var value = (Point)e.NewValue;
                //TODO: Handle new value. 
            }
        }

        #endregion TileCount Dependency Property

        public TileEffect()
        {
            PixelShader pixelShader = new PixelShader();
            pixelShader.UriSource = new Uri("/SilverlightTiledImageBrush;component/Tile.ps", UriKind.Relative);
            this.PixelShader = pixelShader;

            this.UpdateShaderValue(InputProperty);
            this.UpdateShaderValue(TileCountProperty);
        }
        public Brush Input
        {
            get
            {
                return ((Brush)(this.GetValue(InputProperty)));
            }
            set
            {
                this.SetValue(InputProperty, value);
            }
        }

        #region RepeatXY Dependency Property

        public static RepeatXYEnum GetRepeatXY(DependencyObject obj)
        {
            return (RepeatXYEnum)obj.GetValue(RepeatXYProperty);
        }

        public static void SetRepeatXY(DependencyObject obj, RepeatXYEnum value)
        {
            obj.SetValue(RepeatXYProperty, value);
        }

        // Using a DependencyProperty as the backing store for RepeatXY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RepeatXYProperty =
            DependencyProperty.RegisterAttached("RepeatXY", typeof(RepeatXYEnum), typeof(Image), new PropertyMetadata(RepeatXYEnum.None, OnRepeatXYChanged));

        private static void OnRepeatXYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Image image = (Image)d;
            if (image.Effect == null)
            {
                TileEffect tileEffect;
                image.Effect = tileEffect = new TileEffect();
                image.CacheMode = new BitmapCache();
                tileEffect.TileCount = new Point(0.8, 6); // hack

                image.SizeChanged += (s2, e2) =>
                {
                    var bitmap = ((System.Windows.Media.Imaging.BitmapSource)(image.Source));
                    var height = bitmap.PixelHeight;
                    var width = bitmap.PixelWidth;

                    tileEffect.TileCount = new Point(e2.NewSize.Width / width, e2.NewSize.Height / height);
                };
            }
        }


        public enum RepeatXYEnum
        {
            None,
            RepeatX,
            RepeatY,
            RepeatXY
        }

        #endregion RepeatXY Dependency Property


    }

    #region Tile.Mode="Tile" DependencyProperty
    public enum TileMode
    {
        None,
        Tile,
    }

    public class Tile
    {
        public static TileMode GetMode(DependencyObject obj)
        {
            return (TileMode)obj.GetValue(ModeProperty);
        }

        public static void SetMode(DependencyObject obj, TileMode value)
        {
            obj.SetValue(ModeProperty, value);
        }

        // Using a DependencyProperty as the backing store for Mode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached("Mode", typeof(TileMode), typeof(Control), new PropertyMetadata(TileMode.None, OnModeChanged2));

        private static void OnModeChanged2(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Control control = (Control)d;

            control.LayoutUpdated += (s2, e2) =>
            {
                // Check Background after LayoutUpdated because control.Background
                // might not be initialized when TileMode is set.
                if (!(control.Background is ImageBrush)) return;

                foreach (var el in control.GetVisualDescendants())
                {
                    if (el is Panel)
                    {
                        if (TryAddBackground(el, (el as Panel).Background, control.Background)) break;
                    }
                    if (el is Shape)
                    {
                        if (TryAddBackground(el, (el as Shape).Fill, control.Background)) break;
                    }
                }
            };
        }

        private static bool TryAddBackground(FrameworkElement el, Brush brush, Brush originalBrush)
        {
            if (brush != originalBrush) return false;

            Image image = new Image()
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Stretch = System.Windows.Media.Stretch.Fill,
                Source = (originalBrush as ImageBrush).ImageSource,
            };

            if (el is Shape)
            {
                (el as Shape).Fill = new SolidColorBrush(Colors.Transparent);
                el.Clip = new RectangleGeometry()
                {
                    Rect = new Rect(0, 0, (el as Rectangle).ActualWidth, (el as Rectangle).ActualHeight),
                    RadiusX = (el as Rectangle).RadiusX,
                    RadiusY = (el as Rectangle).RadiusY,
                };
                el = el.GetVisualAncestors().FirstOrDefault(element => (element is Panel));
            }

            if (el is Panel && 
                (!((el as Panel).Children[0] is Image) || 
                ((el as Panel).Children[0] as Image).Source != (originalBrush as ImageBrush).ImageSource))
            {
                image.SetValue(TileEffect.RepeatXYProperty, TileEffect.RepeatXYEnum.RepeatXY);
                (el as Panel).Children.Insert(0, image);
                return true;
            }

            return false;
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Control el = (Control)d;
            bool imageAdded = false;
            el.LayoutUpdated += (s2, e2) =>
            {
                var backGround = el.Background;
                foreach (var el2 in el.GetVisualDescendants())
                {
                    if (el2 is Panel)
                    {
                        Panel panel = el2 as Panel;
                        if (panel.Background == backGround && backGround != null && backGround is ImageBrush)
                        {
                            if (!(panel.Children[0] is Image) && !imageAdded)
                            {
                                Image image = new Image()
                                {
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    Stretch = System.Windows.Media.Stretch.Fill,
                                    Source = (backGround as ImageBrush).ImageSource,
                                };
                                image.SetValue(TileEffect.RepeatXYProperty, TileEffect.RepeatXYEnum.RepeatXY);
                                imageAdded = true;
                                panel.Children.Insert(0, image);
                            }
                        }
                        var be = el2.GetBindingExpression(Panel.BackgroundProperty);
                        if (be != null)
                        {
                            var t = be.GetType();
                        }
                    }
                }
            };
        }

    }
    #endregion

}
