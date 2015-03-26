using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using RippleCommonUtilities;
using RippleDictionary;
using ComboBox = System.Windows.Controls.ComboBox;
using HelperMethods = RippleEditor.Utilities.HelperMethods;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace RippleEditor.Controls
{
    /// <summary>
    /// Interaction logic for ApplicationPropertiesControl.xaml
    /// </summary>
    public partial class ApplicationPropertiesControl : UserControl
    {
        private static String prevSelectedUnlockMode = String.Empty;

        public ApplicationPropertiesControl()
        {
            InitializeComponent();
            InitializeControls();
        }

        #region Common Control Methods
        public void InitializeControls()
        {
            try
            {
                //Initialize floor animation types
                CBAnimationTypeValue.Items.Clear();
                foreach (var animType in Enum.GetNames(typeof(AnimationType)))
                {
                    CBAnimationTypeValue.Items.Add(animType);
                }
                CBAnimationTypeValue.SelectedValue = AnimationType.HTML.ToString();

                //Initialize unlock modes
                CBUnlockModeValue.Items.Clear();
                foreach (var unMode in Enum.GetNames(typeof(Mode)))
                {
                    CBUnlockModeValue.Items.Add(unMode);
                }
                prevSelectedUnlockMode = Mode.Gesture.ToString();
                CBUnlockModeValue.SelectedValue = Mode.Gesture.ToString();

                //Initialize gesture unlock types for gesture as the selected unlock mode
                CBUnlockTypeValue.Items.Clear();
                foreach (var unType in Enum.GetNames(typeof(GestureUnlockType)))
                {
                    CBUnlockTypeValue.Items.Add(unType);
                }
                CBUnlockTypeValue.SelectedValue = GestureUnlockType.LeftSwipe.ToString();

                TBAnimationContentValue.Text = "";
                TBLockScreenContentValue.Text = "";
                TBAnimationContentValue.ToolTip = "";
                TBLockScreenContentValue.ToolTip = "";
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in InitializeControls: {0}", ex.Message);
            }
        }

        public bool ValidateControl()
        {
            try
            {
                //Animation Content cannot be empty
                var animationContent = TBAnimationContentValue.Text;
                if (String.IsNullOrEmpty(animationContent))
                {
                    MessageBox.Show("Please enter valid value for Animation URI, as it is required in case of Animation Type = Flash / HTML");
                    return false;
                }

                //Validate the value for Animation content
                //Web URI
                if (CBAnimationTypeValue.SelectedValue.ToString().Equals("HTML") && animationContent.StartsWith("http"))
                {
                    //Do nothing - Valid
                    /*([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?*/
                }
                else
                {
                    //Local HTML URI
                    if (CBAnimationTypeValue.SelectedValue.ToString().Equals(AnimationType.HTML.ToString()) && !(animationContent.EndsWith(".htm") || animationContent.EndsWith(".html")))
                    {
                        MessageBox.Show("Please enter valid value for HTML based URI, it should have an extension html or htm for local files, for web hosted files it should start with http");
                        return false;
                    }
                    ////Local swf file
                    //else if(this.CBAnimationTypeValue.SelectedValue.ToString().Equals(RippleDictionary.AnimationType.Flash.ToString()) && (!animationContent.EndsWith(".swf")))
                    //{
                    //    MessageBox.Show("Please enter valid value for Flash based URI, it should have an extension swf only");
                    //    return false;
                    //}
                }

                //Lock Screen Content cannot be empty
                if (String.IsNullOrEmpty(TBLockScreenContentValue.Text))
                {
                    MessageBox.Show("Please enter valid value for Lock Screen Content URI, as it is required");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in ValidateControl: {0}", ex.Message);
                return false;
            }
        }

        public void SaveApplicationProperties()
        {
            try
            {
                //Animation type
                MainPage.rippleData.Floor.Start.Animation.AnimType = (AnimationType)CBAnimationTypeValue.SelectedIndex;

                //Unlock Mode
                MainPage.rippleData.Floor.Start.Unlock.Mode = (Mode)CBUnlockModeValue.SelectedIndex;

                //Animation content
                MainPage.rippleData.Floor.Start.Animation.Content = TBAnimationContentValue.Text;

                //Lock screen content
                MainPage.rippleData.Screen.ScreenContents["LockScreen"].Content = TBLockScreenContentValue.Text;

                try
                {
                    //Unlock type
                    MainPage.rippleData.Floor.Start.Unlock.UnlockType = CBUnlockTypeValue.SelectedValue.ToString();
                }
                catch(Exception)
                {}
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in SaveApplicationProperties: {0}", ex.Message);
            }           
        }

        public void SetApplicationProperties()
        {
            try
            {
                //Set the Animation Type
                CBAnimationTypeValue.SelectedValue = MainPage.rippleData.Floor.Start.Animation.AnimType.ToString();

                //Set the Unlock Mode
                CBUnlockModeValue.SelectedValue = MainPage.rippleData.Floor.Start.Unlock.Mode.ToString();
                CBUnlockModeValue.SelectedValue = MainPage.rippleData.Floor.Start.Unlock.Mode.ToString();

                //Set the Unlock Type
                CBUnlockTypeValue.SelectedValue = MainPage.rippleData.Floor.Start.Unlock.UnlockType.ToString();

                //Set the Animation content
                if (!MainPage.rippleData.Floor.Start.Animation.Content.StartsWith(@"\Assets\"))
                    TBAnimationContentValue.Text = MainPage.rippleData.Floor.Start.Animation.Content;
                else
                    TBAnimationContentValue.Text = HelperMethods.TargetAssetsRoot + MainPage.rippleData.Floor.Start.Animation.Content;

                //Set the Lock Screen content
                if (!MainPage.rippleData.Screen.ScreenContents["LockScreen"].Content.StartsWith(@"\Assets\"))
                    TBLockScreenContentValue.Text = MainPage.rippleData.Screen.ScreenContents["LockScreen"].Content;
                else
                    TBLockScreenContentValue.Text = HelperMethods.TargetAssetsRoot + MainPage.rippleData.Screen.ScreenContents["LockScreen"].Content;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in SetApplicationProperties: {0}", ex.Message);
            }
        } 
        #endregion

        #region UI Methods
        private void AnimationContentBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //if (this.CBAnimationTypeValue.SelectedValue == RippleDictionary.AnimationType.Flash.ToString())
                //{
                //    //Opportunity to browse for Animation files
                //    System.Windows.Forms.OpenFileDialog dlgBox = new System.Windows.Forms.OpenFileDialog();
                //    dlgBox.Filter = "Flash files(*.swf;)|*.swf;";
                //    var res = dlgBox.ShowDialog();
                //    if (res == System.Windows.Forms.DialogResult.OK)
                //    {
                //        String updatedFileName = Utilities.HelperMethods.CopyFile(dlgBox.FileName, Utilities.HelperMethods.TargetAssetsDirectory + "\\Animations");

                //        if (!String.IsNullOrEmpty(updatedFileName))
                //        {
                //            this.TBAnimationContentValue.Text = updatedFileName;
                //            this.TBAnimationContentValue.ToolTip = updatedFileName;
                //        }
                //        else
                //        {
                //            this.TBAnimationContentValue.Text = "";
                //            this.TBAnimationContentValue.ToolTip = "";
                //        }
                //    }
                //}
                //else 
                if (CBAnimationTypeValue.SelectedValue == AnimationType.HTML.ToString())
                {
                    //Opportunity to browse for Animation files
                    var dlgBox = new OpenFileDialog();
                    dlgBox.Filter = "HTML files(*.html;*.htm;)|*.html;*.htm;";
                    var res = dlgBox.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        var updatedFileName = dlgBox.FileName;
                        var targetfolder = HelperMethods.CopyFolder(Path.GetDirectoryName(updatedFileName), HelperMethods.TargetAssetsDirectory + "\\Animations");
                        updatedFileName = targetfolder + "\\" + Path.GetFileName(updatedFileName);
                        if (!String.IsNullOrEmpty(updatedFileName))
                        {
                            TBAnimationContentValue.Text = updatedFileName;
                            TBAnimationContentValue.ToolTip = updatedFileName;
                        }
                        else
                        {
                            TBAnimationContentValue.Text = "";
                            TBAnimationContentValue.ToolTip = "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in AnimationContentBrowseButton_Click: {0}", ex.Message);
            }
        }

        //TODO: Lock screen content could support other content types as well.
        private void LockScreenContentBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Opportunity to browse for content files
                var dlgBox = new OpenFileDialog();
                dlgBox.Filter = "Media Files(*.mp4;*.wmv;*.jpeg;*.png;*.jpg;*.bmp;)|*.mp4;*.wmv;*.jpeg;*.png;*.jpg;*.bmp;|Videos(*.mp4;*.wmv;)|*.mp4;*.wmv;|Images(*.jpeg;*.png;*.jpg;*.bmp;)|*.jpeg;*.png;*.jpg;*.bmp;";

                var res = dlgBox.ShowDialog();
                if (res == DialogResult.OK)
                {
                    var targetFolder = HelperMethods.TargetAssetsDirectory;
                    var fileExt = Path.GetExtension(dlgBox.FileName).ToLower();
                    if (fileExt.Equals(".mp4") || fileExt.Equals(".wmv"))
                        targetFolder += "\\Videos";
                    else
                        targetFolder += "\\Images";
                    //Get the complete fileName
                    var updatedFileName = HelperMethods.CopyFile(dlgBox.FileName, targetFolder);
                    if (!String.IsNullOrEmpty(updatedFileName))
                    {
                        TBLockScreenContentValue.Text = updatedFileName;
                        TBLockScreenContentValue.ToolTip = updatedFileName;
                    }
                    else
                    {
                        TBLockScreenContentValue.Text = "";
                        TBLockScreenContentValue.ToolTip = "";
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in LockScreenContentBrowseButton_Click: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Selection Changed event for the Unlock modes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CBUnlockModeValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                if (cb.SelectedValue == null)
                    return;

                //Gesture selected
                if (cb.SelectedValue.ToString() == Mode.Gesture.ToString())
                {
                    var g = CBUnlockModeValue.SelectedValue.ToString();
                    //Get the previously selected unlock mode
                    prevSelectedUnlockMode = CBUnlockModeValue.SelectedValue.ToString();

                    //Set the unlock types as gesture types - select Right Swipe as default
                    CBUnlockTypeValue.Items.Clear();
                    foreach (var unType in Enum.GetNames(typeof(GestureUnlockType)))
                    {
                        CBUnlockTypeValue.Items.Add(unType);
                    }
                    CBUnlockTypeValue.SelectedValue = GestureUnlockType.RightSwipe.ToString();
                }
                //Animation Selected
                else if (cb.SelectedValue.ToString() == Mode.HTML.ToString())
                {
                    //Get the previously selected unlock mode
                    prevSelectedUnlockMode = CBUnlockModeValue.SelectedValue.ToString();

                    //Set the unlock types as HTMl invoke
                    CBUnlockTypeValue.Items.Clear();
                    CBUnlockTypeValue.Items.Add("HTMLInvoke");
                    CBUnlockTypeValue.SelectedValue = "HTMLInvoke";
                }
                else
                {
                    //Set the selection to previous selected
                    cb.SelectedValue = prevSelectedUnlockMode;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in CBUnlockModeValue_SelectionChanged: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Selection Changed event for type of Unlock inside the modes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CBUnlockTypeValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Do nothing right now
        }

        /// <summary>
        /// Selection changed event for the main Lock Screen Animation Type
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CBAnimationTypeValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                if (cb.SelectedValue == null)
                    return;

                //if (cb.SelectedValue.ToString() == RippleDictionary.AnimationType.Flash.ToString() || cb.SelectedValue.ToString() == RippleDictionary.AnimationType.HTML.ToString())
                //{
                //    //Set unlock mode as gesture
                //    this.CBUnlockModeValue.SelectedValue = RippleDictionary.Mode.Gesture.ToString();

                //    //Set Unlock type as gesture types - default right swipe
                //    this.CBUnlockTypeValue.Items.Clear();
                //    foreach (var unType in Enum.GetNames(typeof(RippleDictionary.GestureUnlockType)))
                //    {
                //        this.CBUnlockTypeValue.Items.Add(unType);
                //    }
                //    this.CBUnlockTypeValue.SelectedValue = RippleDictionary.GestureUnlockType.RightSwipe.ToString();

                //    //Clear the animation content selected
                //    this.TBAnimationContentValue.Text = "";
                //    this.TBAnimationContentValue.ToolTip = "";
                //}
                //else 
                if (cb.SelectedValue.ToString() == AnimationType.HTML.ToString())
                {
                    //Set unlock mode as Animation - default
                    CBUnlockModeValue.SelectedValue = Mode.HTML.ToString();

                    //Set Unlock type as fixed HTML invoke value
                    CBUnlockTypeValue.Items.Clear();
                    CBUnlockTypeValue.Items.Add("HTMLInvoke");

                    CBUnlockTypeValue.SelectedValue = "HTMLInvoke";

                    //Clear the animation content selected
                    TBAnimationContentValue.Text = "";
                    TBAnimationContentValue.ToolTip = "";
                }

            }
            catch (Exception ex)
            {
                LoggingHelper.LogTrace(1, "Went wrong in CBAnimationTypeValue_SelectionChanged: {0}", ex.Message);
            }            
        } 
        #endregion

    }
}
