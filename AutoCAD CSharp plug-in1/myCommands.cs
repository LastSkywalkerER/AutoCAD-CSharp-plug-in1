// (C) Copyright 2021 by  
//
using System;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Windows;
using Exception = Autodesk.AutoCAD.Runtime.Exception;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(AutoCAD_CSharp_plug_in1.MyCommands))]

namespace AutoCAD_CSharp_plug_in1
{
	public class MyCommands
	{
		//даём комманду автокада методу
		[CommandMethod("addAnEnt")]
		public void AddAnEnt()
		{
			//получаем редактор текущего документа
			Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
			//запрашиваем ввод у пользователя
			PromptKeywordOptions getWhichEntityOptions = new PromptKeywordOptions("Чё создаём? [Circle/Block] : ", "Circle Block");
			//получаем строку из редактора
			PromptResult getWichEntityResult = editor.GetKeywords(getWhichEntityOptions);
			//если был ввод, то продалжаем
			if (getWichEntityResult.Status == PromptStatus.OK)
			{

				//получаем базу данных текущего чертежа
				Database dwg = editor.Document.Database;
				//запускаем транзакуию в бд
				Transaction transaction = dwg.TransactionManager.StartTransaction();

				//перебираем опции ввода
				switch (getWichEntityResult.StringResult)
				{
					case "Circle":

						//просим точку у юзверя
						PromptPointOptions getPointOptions = new PromptPointOptions("Клацни по центру : ");
						//получаем точку от юзверя
						PromptPointResult getPointResult = editor.GetPoint(getPointOptions);
						//если точка получена - продолжаем
						if (getPointResult.Status == PromptStatus.OK)
						{
							//просим параметр длины в качестве радиуса у юзверя
							PromptDistanceOptions getRadiusOptions = new PromptDistanceOptions("Бахни радиус : ");
							//получаем значение радиуса
							getRadiusOptions.BasePoint = getPointResult.Value;
							//добавляем базовую точку к радиусу
							getRadiusOptions.UseBasePoint = true;
							//получаем результат получения радиуса (ШТА??)
							PromptDoubleResult getRadiusResult = editor.GetDistance(getRadiusOptions);
							//если получили радиус - продолжаем
							if (getRadiusResult.Status == PromptStatus.OK)
							{

								try
								{
									// создаём окружность по базовой точке, вектору нормали и радиусу
									Circle circle = new Circle(getPointResult.Value, Vector3d.ZAxis, getRadiusResult.Value);
									//открываем текущее пространство для записи
									BlockTableRecord blockTableRecord = (BlockTableRecord)transaction.GetObject(dwg.CurrentSpaceId, OpenMode.ForWrite);
									//вставляем круг в базу данных												
									blockTableRecord.AppendEntity(circle);
									//говорим транзакции о том, что вставили круг									
									transaction.AddNewlyCreatedDBObject(circle, true);
									//подтверждаем транзакцию									
									transaction.Commit();

									editor.WriteMessage("Поздравляю, окружность бахнута через C# ");
								}
								catch (Exception exception)
								{
									//выводим сообщение, в случае возникновения ошибки
									editor.WriteMessage("Косяк :  " + exception.Message.ToString());
								}
								finally
								{
									//удаление всех объектов связанных с транзакцией и самой транзакции
									transaction.Dispose();
								}
							}
						}
						break;


					case "Block":
						//запрашиваем строку-имя блока
						PromptStringOptions blockNameOptions = new PromptStringOptions("Дайте кликуху блоку : ");
						//запрещаем пробелы в имени блока
						blockNameOptions.AllowSpaces = false;
						//получаем строку от эдитора
						PromptResult blockNameResult = editor.GetString(blockNameOptions);


						try
						{
							//создаём пустой блок
							BlockTableRecord newBlockDef = new BlockTableRecord();
							//даём имя пустому блоку
							newBlockDef.Name = blockNameResult.StringResult;
							//открываем бд на чтение
							BlockTable blockTable = (BlockTable)transaction.GetObject(dwg.BlockTableId, OpenMode.ForRead);
							//проверяем, что не существует блока с таким именем
							if (!blockTable.Has(blockNameResult.StringResult))
							{
								//апргрейдим доступ к бд на запись
								blockTable.UpgradeOpen();
								//добавляем определение блока в бд
								blockTable.Add(newBlockDef);
								//говорим транзакции о добавлении блока
								transaction.AddNewlyCreatedDBObject(newBlockDef, true);

								editor.WriteMessage(" Красавчег, блок" + blockNameResult.StringResult + " создан ");

								//создаём круги
								Circle circle1 = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
								newBlockDef.AppendEntity(circle1);
								Circle circle2 = new Circle(new Point3d(20, 10, 0), Vector3d.ZAxis, 10);
								newBlockDef.AppendEntity(circle2);
								transaction.AddNewlyCreatedDBObject(circle1, true);
								transaction.AddNewlyCreatedDBObject(circle2, true);
								//запрашиваем точку вставки блока
								PromptPointOptions blockRefPointOptions = new PromptPointOptions(" Де бахать блок? : ");
								//получаем точку вставки
								PromptPointResult blockRefPointResult = editor.GetPoint(blockRefPointOptions);
								//если точка не получена, останавливаем транзакцию и вылазиЕм из блока кода
								if (blockRefPointResult.Status != PromptStatus.OK)
								{
									transaction.Dispose();
									return;
								}

								//создаём вхождение блока с точкой и описанием блока
								BlockReference blockReference = new BlockReference(blockRefPointResult.Value, newBlockDef.ObjectId);
								//открываем текущее пространство на чтение (раньше получали текущую бд)
								BlockTableRecord curSpace = (BlockTableRecord)transaction.GetObject(dwg.CurrentSpaceId, OpenMode.ForWrite);
								//вставляем вхождение блока в текущее пространство
								curSpace.AppendEntity(blockReference);
								//говорим транзакции о вставке вхождения блока
								transaction.AddNewlyCreatedDBObject(blockReference, true);
								//подтверждаем транзакцию
								transaction.Commit();

								editor.WriteMessage(" Красавчег, блок" + blockNameResult.StringResult + " вставлен ");

							}
						}

						catch (Exception exception)
						{
							//сообщаем об ошибке
							editor.WriteMessage(" Косяк : " + exception.Message.ToString());
						}

						finally
						{
							//очищаем транзакцию
							transaction.Dispose();
						}

						break;
				}
			}
		}

		//объявляем набор палитр
		public PaletteSet myPaletteSet;
		//объявляем палитру
		public UserControl1 myPalette;
		//задаём команду
		[CommandMethod("Palette")]
		public void palette()
		{
			//проверяем, что набор палитр ещё не создана
			if (myPaletteSet == null)
			{
				//создаём набор палитр с уникальным гуид
				myPaletteSet = new PaletteSet("My SkyPalette", new Guid("D7467BCE-EA62-4709-BF36-206ADA8DBE19"));
				//закидываем в переменную созданную форму
				myPalette = new UserControl1();
				//добавление формы/палитры в набор палитр (необходимо в конструкторе формы явно ук5азать поддержку прозрачности this.SetStyle(System.Windows.Forms.ControlStyles.SupportsTransparentBackColor, true);)
				try
				{
					myPaletteSet.Add("SkyPalette1", myPalette);
				}
				catch (System.Exception exception)
				{
					Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
					editor.WriteMessage("\n не удалось бахнуть палитру : " + exception);
				}


			}
			//показываем набор палитр
			myPaletteSet.Visible = true;

		}

		//команда для отслеживания событий автокада
		[CommandMethod("AddDbEvents")]
		public void addDbEvents()
		{
			//проверяем создана ли палитра и выходим из метода, если нет
			if (myPalette == null)
			{
				Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
				editor.WriteMessage("\n Бахни в начале комманду 'Palette'");
				return;
			}

			//получаем базу текущего чертежа
			Database curDwg = Application.DocumentManager.MdiActiveDocument.Database;

			//вешаем слушатель на создание объекта
			curDwg.ObjectAppended += new ObjectEventHandler(callback_ObjectAppended);
			//вешаем слушатель на удаление объекта
			curDwg.ObjectErased += new ObjectErasedEventHandler(callback_ObjectErased);
			//вешаем слушатель на пересоздание объекта
			curDwg.ObjectReappended += new ObjectEventHandler(callback_ObjectAppended);
			//вешаем слушатель на антисоздание (ШТА??) объекта
			curDwg.ObjectUnappended += new ObjectEventHandler(callback_ObjectUnappended);

		}
		//метод вызываемый при создании объекта
		private void callback_ObjectAppended(object sender, ObjectEventArgs e)
		{
			//добавляем узел в дерево и присваеваем его переменной
			TreeNode newNode = myPalette.treeView1.Nodes.Add(e.DBObject.GetType().ToString());
			//вешаем тег на созданный узел
			newNode.Tag = e.DBObject.ObjectId.ToString();

			//throw new NotImplementedException();
		}

		private void callback_ObjectErased(object sender, ObjectErasedEventArgs e)
		{
			//смотрим, что при удалении, эвент тоже удаление (ШТА??)
			if (e.Erased)
			{
				//перебирваем все узлы и сравниваем по тегу и обжект ид, затем удаляем
				foreach (TreeNode node in myPalette.treeView1.Nodes)
				{
					if ((string)node.Tag == e.DBObject.ObjectId.ToString())
					{
						node.Remove();
						break;
					}
				}

			}
			//если эвент не удаление, то почему-то добавляем узел
			else
			{
				//добавляем узел в дерево и присваеваем его переменной
				TreeNode newNode = myPalette.treeView1.Nodes.Add(e.DBObject.GetType().ToString());
				//вешаем тег на созданный узел
				newNode.Tag = e.DBObject.ObjectId.ToString();
			}


			//throw new NotImplementedException();
		}
		//метод на антисоздание объекта
		private void callback_ObjectUnappended(object sender, ObjectEventArgs e)
		{
			//перебирваем все узлы и сравниваем по тегу и обжект ид, затем удаляем
			foreach (TreeNode node in myPalette.treeView1.Nodes)
			{
				if ((string)node.Tag == e.DBObject.ObjectId.ToString())
				{
					node.Remove();
					break;
				}
			}

			//throw new NotImplementedException();
		}

		//команда на добавление инфы в словари связанные с объектами
		[CommandMethod("addData")]
		public void addData()
		{

			// get the editor object 
			Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
			// pick entity to add data to! 
			PromptEntityResult getEntityResult = ed.GetEntity("Pick an entity to add an Extension Dictionary to : ");
			// if all was ok 
			if ((getEntityResult.Status == PromptStatus.OK))
			{
				// now start a transaction 
				Transaction trans = ed.Document.Database.TransactionManager.StartTransaction();
				try
				{
					//получаем выделенный объект для чтения (наверно если их несколько не сработает и нужен перебор)
					Entity ent = (Entity)trans.GetObject(getEntityResult.ObjectId, OpenMode.ForRead);
					//проверяем, что у объекта нет связанного словаря
					if (ent.ExtensionDictionary.IsNull)
					{
						//открываем объект на запись
						ent.UpgradeOpen();
						//добавляем словарь к объекту
						ent.CreateExtensionDictionary();
					}
					//получаем связанные с объектом словарь на чтение
					DBDictionary extensionDict = (DBDictionary)trans.GetObject(ent.ExtensionDictionary, OpenMode.ForRead);
					//проверяем есть ли в словаре наша инфа
					if (extensionDict.Contains("MyData"))
					{
						//получаем ид нашей инфы в словаре
						ObjectId entryId = extensionDict.GetAt("MyData");
						//сообщаем о наличии инфы
						ed.WriteMessage("\n В объекте уже есть инфа");
						//получаем нашу инфу из словаря на чтение
						Xrecord myXrecord = (Xrecord)trans.GetObject(entryId, OpenMode.ForRead);
						//пробегаемся по списку из нашей инфы
						foreach (TypedValue value in myXrecord.Data)
						{
							//выводим построчно инфу из инфы
							ed.WriteMessage("\n" + value.TypeCode.ToString() + "." + value.Value.ToString());
						}
					}
					//действия на случай отсутвия у объекта инфы
					else
					{
						//обновляем объект на запись
						extensionDict.UpgradeOpen();
						//создаём новую запись словаря
						Xrecord myXrecord = new Xrecord();
						//создаём новый список инфы для словаря
						ResultBuffer data = new ResultBuffer(new TypedValue((int)DxfCode.Int16, 1),
															 new TypedValue((int)DxfCode.Text, "MyStockData"),
															 new TypedValue((int)DxfCode.Real, 51.9),
															 new TypedValue((int)DxfCode.Real, 100.0),
															 new TypedValue((int)DxfCode.Real, 320.6)
															 );
						//закидываем инфу в словарь
						myXrecord.Data = data;
						//закидываем наш словарь в словарь объекта
						extensionDict.SetAt("MyData", myXrecord);
						//говорим транзакции о добавлении инфы в словарь
						trans.AddNewlyCreatedDBObject(myXrecord, true);
						//проверяем создана ли палитра
						if (myPalette != null)
						{
							//перебирваем узлы в палитре
							foreach (TreeNode node in myPalette.treeView1.Nodes)
							{
								//ищем выделенный объект в палитре
								if ((string)node.Tag == ent.ObjectId.ToString())
								{
									//создаём дочерний узел для доп инфы по словарю
									TreeNode childNode = node.Nodes.Add("Extension Dictionary");
									//для ккаждой записи в словаре добавляем строку в палитру
									foreach (TypedValue value in myXrecord.Data)
									{
										childNode.Nodes.Add(value.ToString());
									}
									break;
								}
							}
						}
					}

					trans.Commit();
				}
				catch (Exception ex)
				{
					// a problem occured, lets print it 
					ed.WriteMessage("a problem occured because " + ex.Message);
				}
				finally
				{
					// whatever happens we must dispose the transaction 

					trans.Dispose();

				}

			}
		}

		//команда на добавление инфы в глобальный словарь чертежа
		[CommandMethod("addDataToNOD")]
		public void addDataToNOD()
		{

			// get the editor object 
			Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
			// pick entity to add data to! 
			Transaction trans = ed.Document.Database.TransactionManager.StartTransaction();
			try
			{
				//получаем глобальный словарь на чтение
				DBDictionary nod = (DBDictionary)trans.GetObject(ed.Document.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
				//проверяем есть ли в словаре уже есть наша инфа
				if (nod.Contains("MyData"))
				{
					//получаем ид нашей инфы
					ObjectId entryId = nod.GetAt("MyData");
					//говорим о наличии
					ed.WriteMessage("\n на уровне чертежа (NOD) наша инфа уже есть");
					//получаем нашу инфу на чтение
					Xrecord myXrecord = (Xrecord)trans.GetObject(entryId, OpenMode.ForRead);
					//перебираем список в нашей инфе и выводим построчно
					foreach (TypedValue value in myXrecord.Data)
					{
						ed.WriteMessage("\n" + value.TypeCode.ToString() + " . " + value.Value.ToString());
					}
				}
				else
				{
					//обновляем глобальный словарь на запись
					nod.UpgradeOpen();
					//создаём новый словарь
					Xrecord myXrecord = new Xrecord();
					//создаём новую запись словаря
					ResultBuffer data = new ResultBuffer(new TypedValue((int)DxfCode.Int16, 1),
															 new TypedValue((int)DxfCode.Text, "MyStockData"),
															 new TypedValue((int)DxfCode.Real, 51.9),
															 new TypedValue((int)DxfCode.Real, 100.0),
															 new TypedValue((int)DxfCode.Real, 320.6)
															 );
					//добавляем запись в словарь
					myXrecord.Data = data;
					//добавляем наш словарь в глобальный
					nod.SetAt("MyData", myXrecord);
					//говорим транзакции о добавлении инфы в словарь
					trans.AddNewlyCreatedDBObject(myXrecord, true);
				}

				trans.Commit();
			}
			catch (Exception ex)
			{
				// a problem occurred, lets print it 
				ed.WriteMessage("a problem occurred because " + ex.Message);
			}
			finally
			{
				// whatever happens we must dispose the transaction 

				trans.Dispose();

			}
		}

		[CommandMethod("addPointMonitor")]
		public void startMonitor()
		{
			Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

			editor.PointMonitor += new PointMonitorEventHandler(MyPointMonitor);
		}

		private void MyPointMonitor(object sender, PointMonitorEventArgs e)
		{
			FullSubentityPath[] fullEntPath = e.Context.GetPickedEntities();

			if (fullEntPath.Length > 0)
			{
				Transaction transaction = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();

				try
				{
					Entity ent = (Entity)transaction.GetObject(fullEntPath[0].GetObjectIds()[0], OpenMode.ForRead);

					e.AppendToolTipText("эта штука есть " + ent.GetType().ToString());

					if (myPalette == null)
					{
						return;
					}

					System.Drawing.Font fontRegular = new System.Drawing.Font("Microsoft Sans Serif", 8, System.Drawing.FontStyle.Regular);
					System.Drawing.Font fontBold = new System.Drawing.Font("Microsoft Sans Serif", 8, System.Drawing.FontStyle.Bold);

					myPalette.treeView1.SuspendLayout();

					foreach (TreeNode node in myPalette.treeView1.Nodes)
					{
						if ((string)node.Tag == ent.ObjectId.ToString())
						{
							if (!fontBold.Equals(node.NodeFont))
							{
								node.NodeFont = fontBold;

								node.Text = node.Text;
							}
						}
						else
						{
							if (!fontRegular.Equals(node.NodeFont))
							{
								node.NodeFont = fontRegular;

							}

						}
					}

					myPalette.treeView1.ResumeLayout();
					myPalette.treeView1.Refresh();
					myPalette.treeView1.Update();

					transaction.Commit();
				}
				catch (Exception ex)
				{
					Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.ToString());
				}
				finally
				{
					transaction.Dispose();
				}
			}

			//throw new NotImplementedException();
		}

		[CommandMethod("newInput")]
		public void NewInput()
		{
			// start our input point Monitor    
			// get the editor object
			Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

			// now add the delegate to the events list
			ed.PointMonitor += new PointMonitorEventHandler(MyInputMonitor);

			ed.TurnForcedPickOn();

			PromptPointOptions getPointOptions = new PromptPointOptions("клацни де нить : ");

			PromptPointResult getPointResult = ed.GetPoint(getPointOptions);

			ed.PointMonitor -= new PointMonitorEventHandler(MyInputMonitor);

		}

		public void MyInputMonitor(object sender, PointMonitorEventArgs e)
		{
			if (e.Context == null)
			{
				return;
			}

			//  first lets check what is under the Cursor
			FullSubentityPath[] fullEntPath = e.Context.GetPickedEntities();
			if (fullEntPath.Length > 0)
			{
				//  start a transaction
				Transaction trans = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
				try
				{
					//  open the Entity for read, it must be derived from Curve
					Curve ent = (Curve)trans.GetObject(fullEntPath[0].GetObjectIds()[0], OpenMode.ForRead);

					//  ok, so if we are over something - then check to see if it has an extension dictionary
					if (ent.ExtensionDictionary.IsValid)
					{
						// open it for read
						DBDictionary extensionDict = (DBDictionary)trans.GetObject(ent.ExtensionDictionary, OpenMode.ForRead);

						// find the entry
						ObjectId entryId = extensionDict.GetAt("MyData");

						// if we are here, then all is ok
						// extract the xrecord
						Xrecord myXrecord;

						//  read it from the extension dictionary
						myXrecord = (Xrecord)trans.GetObject(entryId, OpenMode.ForRead);

						foreach (TypedValue myTypedValue in myXrecord.Data)
						{
							if ((DxfCode)myTypedValue.TypeCode == DxfCode.Real)
							{
								Point3d point = ent.GetPointAtDist((double)myTypedValue.Value);

								Point2d pixels = e.Context.DrawContext.Viewport.GetNumPixelsInUnitSquare(point);

								double xDist = 10 / pixels.X;
								double yDist = 10 / pixels.Y;

								Circle circle = new Circle(point, Vector3d.ZAxis, xDist);

								e.Context.DrawContext.Geometry.Draw(circle);

								DBText text = new DBText();

								text.SetDatabaseDefaults();

								text.Position = point + new Vector3d(xDist, yDist, 0);

								text.TextString = myTypedValue.Value.ToString();

								text.Height = yDist;

								e.Context.DrawContext.Geometry.Draw(text);
							}
						}
					}
					//  all ok, commit it
					trans.Commit();
				}
				catch (Exception ex)
				{
					Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.ToString());
				}
				finally
				{
					//  whatever happens we must dispose the transaction
					trans.Dispose();
				}
			}
		}
	}
}

