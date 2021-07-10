using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCAD_CSharp_plug_in1
{
	public partial class UserControl1 : UserControl
	{
		public UserControl1()
		{
			InitializeComponent();
			//для лейбла в форме добавляем событие драг
			DragLabel.MouseMove += new System.Windows.Forms.MouseEventHandler(DragLabel_MouseMove);
		}

		private void DragLabel_MouseMove(object sender, MouseEventArgs e)
		{
			//проверяем, что тянем левой кнопкой
			if (Control.MouseButtons == MouseButtons.Left)
			{
				Application.DoDragDrop(this, this, DragDropEffects.All, new myDropTurget());
			}
		}
	}

	//класс описывающий цель дропа
	public class myDropTurget : DropTarget
	{
		//функция выполняющаяся по дропу
		public override void OnDrop(DragEventArgs e)
		{
			Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

			try
			{
				//т к функция выполняется не командной автокада, необходимо заблокировать документ, на момент её выполнения
				using (DocumentLock docLock = Application.DocumentManager.MdiActiveDocument.LockDocument())
				{
					adskClass.AddAnEnt();
				}
			}
			catch (Exception ex)
			{
				ed.WriteMessage("Драг энд дроп не удался " + ex.Message);
			}
		}
	}
}
