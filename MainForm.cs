using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArrowsCursorGUI
{
  public partial class MainForm : Form
  {
    private readonly GlobalKeyboardHook Hook = new GlobalKeyboardHook();
    private readonly Controller Controller = new Controller();
    private bool PrevConsumed;
    
    public MainForm()
    {
      InitializeComponent(); // Must come first.
      InitLevelBoxes();
      InitMainKeyButton();
      InitClickKeyButton();

      disableKeysCheckbox.CheckedChanged += (o, e) => Controller.Consume = disableKeysCheckbox.Checked;
      disableKeysCheckbox.Checked = Controller.Consume;

      disableFirstSpeedLevelCheckbox.Checked = Controller.Speed.FirstDisabled;
      disableFirstSpeedLevelCheckbox.CheckedChanged += (o, e) =>
        LevelBox1.Enabled = !(Controller.Speed.FirstDisabled = disableFirstSpeedLevelCheckbox.Checked);

      AccelerateCheckbox.Checked = Controller.Speed.Accelerate;
      AccelerateCheckbox.CheckedChanged += (o, e) => Controller.Speed.Accelerate = AccelerateCheckbox.Checked; 
      Controller.Run();

      
      // All this duplicate code (along with it's handler methods) could be abstracted and made into a component,
      // but we don't care about it for now. 
      void InitClickKeyButton()
      {
        ClickKeyBtn.Text = new KeysConverter().ConvertToString(Controller.ClickKey.Code);
        ClickKeyBtn.Click += (obj, e) =>
        {
          PrevConsumed = Controller.Consume;
          Controller.Consume = false;
          ClickKeyStateLbl.Text = "Press any key...";
          ClickKeyBtn.Text = "";
          Hook.KeyboardPressed += HandleClickKeyButtonKeyPressed;
        };
      }
          
      void InitMainKeyButton()
      {
        MainKeyBtn.Text = new KeysConverter().ConvertToString(Controller.MainKey.Code);
        MainKeyBtn.Click += (obj, e) =>
        {
          PrevConsumed = Controller.Consume;
          Controller.Consume = false;
          MainKeyStateLbl.Text = "Press any key...";
          MainKeyBtn.Text = "";
          Hook.KeyboardPressed += HandleMainKeyButtonKeyPressed;
        };
      }
      
      void InitLevelBoxes()
      {
        NumericUpDown[] levelBoxes = { LevelBox1, LevelBox2, LevelBox3 };

        for (byte i = 0; i < levelBoxes.Length; i++)
        {
          levelBoxes[i].Value = Controller.Speed.Levels[i];
          int iCopy = i;
          levelBoxes[i].ValueChanged += (obj, e) => Controller.Speed.ChangeLevelValue(iCopy, (int)levelBoxes[iCopy].Value);
        }

        Controller.SpeedChanged += (obj, e) =>
        {
          for (byte i = 0; i < levelBoxes.Length; i++)
            levelBoxes[i].BackColor = Controller.Speed.CurrentLevel == i ? Color.FromArgb(240, 220, 220) : Color.White;
        };
      }
    }
    
    
    private void HandleClickKeyButtonKeyPressed(object o, GlobalKeyboardHookEventArgs e)
    {
      Controller.Consume = PrevConsumed;
      Hook.KeyboardPressed -= HandleClickKeyButtonKeyPressed;
      ClickKeyStateLbl.Text = "Click to change";
      ClickKeyBtn.Text = new KeysConverter().ConvertToString(e.KeyboardData.VirtualCode);
      Controller.ClickKey.Code = e.KeyboardData.VirtualCode;
    }
    
    private void HandleMainKeyButtonKeyPressed(object o, GlobalKeyboardHookEventArgs e)  
    {
      Controller.Consume = PrevConsumed;
      Hook.KeyboardPressed -= HandleMainKeyButtonKeyPressed;
      MainKeyStateLbl.Text = "Click to change";
      MainKeyBtn.Text = new KeysConverter().ConvertToString(e.KeyboardData.VirtualCode);
      Controller.MainKey.Code = e.KeyboardData.VirtualCode;
    }
  }
}
