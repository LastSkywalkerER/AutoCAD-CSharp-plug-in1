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
using MenuItem = Autodesk.AutoCAD.Windows.MenuItem;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(AutoCAD_CSharp_plug_in1.adskClass))]

namespace AutoCAD_CSharp_plug_in1
{
	//IExtensionApplication говорит автокаду, что это встраиваемое расширение и добавляет функционал по его инициализации и удалению
	public class adskClass : IExtensionApplication
	{
		//переменная для пункта меню
		ContextMenuExtension myContextMenu;
		//метод создания пункта меню
		private void addContextMenu()
		{
			Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

			try
			{
				//создаём экземпляр меню
				myContextMenu = new ContextMenuExtension();
				//даём название
				myContextMenu.Title = "Circle jig";
				//создаём экземпляр пункта меню
				MenuItem mi = new MenuItem("Run Circle Jig");
				//добавляем обработку клика по пункту меню
				mi.Click += CallBackOnClick;
				//добавляем пункт в меню
				myContextMenu.MenuItems.Add(mi);
				//добавляем меню в автокад
				Application.AddDefaultContextMenuExtension(myContextMenu);
			}
			catch (Exception ex)
			{
				ed.WriteMessage("ошибка с контекстным меню " + ex.Message);
			}
		}
		//описываем удаления меню
		public void RemoveContextMenu()
		{
			Document activeDoc = Application.DocumentManager.MdiActiveDocument;

			try
			{
				if (myContextMenu != null)
				{
					//убираем меню из автокада
					Application.RemoveDefaultContextMenuExtension(myContextMenu);
					//обнуляем наше меню
					myContextMenu = null;
				}
			}
			catch (Exception ex)
			{
				activeDoc.Editor.WriteMessage("ошибка с удалением контекстным меню " + ex.Message);
			}
		}
		//опиываем действия по клику на пункт меню
		private void CallBackOnClick(object sender, System.EventArgs e)
		{
			//т к объект рисуется не из под автокадовской команды, необходимо заблокировать документ на момент её выполнения
			using (DocumentLock docLock = Application.DocumentManager.MdiActiveDocument.LockDocument())
			{
				circleJig();
			}

			
		}

		//метод, который должен вызывать по загрузке dll
		[CommandMethod("Initialize")]
		public void Initialize()
		{
			Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n Инициализация... \n");
			//добавляем пункт меню автокада (пкм по рабочей области)
			addContextMenu();
			//добавляем вкладку в настрйоки автокада (options)
			AdTabDialog();
		}

		public void Terminate()
		{
			Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n Устранение... \n");
			//метод устраняющий меню автокада
			RemoveContextMenu();
		}
		//переменная для теста вкладки настроек автокада, в неё записываем данные из вкладки
		public static string myVariable;
		//создаём вкладку в настройках, описываем событие по открытии настроек
		private static void AdTabDialog()
		{
			Application.DisplayingOptionDialog += TabHandler;
		}

		private static void TabHandler(object sender, TabbedDialogEventArgs e)
		{
			//получаем форму
			myCustomTab myCustomTab = new myCustomTab();
			//получаем действие по нажатию на ок
			TabbedDialogAction tabbedDialogAct = new TabbedDialogAction(myCustomTab.OnOk);
			//описываем работу вкладки, форму и кнопку ок
			TabbedDialogExtension tabbedDialogExt = new TabbedDialogExtension(myCustomTab, tabbedDialogAct);
			//добавляем вкладку в окно
			e.AddTab("Кастомное значение", tabbedDialogExt);
		}
		//проверям работу вкладки настроек, через отображение введённой там информации
		[CommandMethod("testTab")]
		public void TestTab()
		{
			Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
			ed.WriteMessage(myVariable.ToString());
		}



		//даём комманду автокада методу
		[CommandMethod("addAnEnt")]
		public static void AddAnEnt()
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
		//создаём команду на описание объекта под курсором
		[CommandMethod("addPointMonitor")]
		public void startMonitor()
		{
			Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
			//вешаем обработчик события на движение мыши
			editor.PointMonitor += new PointMonitorEventHandler(MyPointMonitor);
		}
		//сам обработчик по движению мыши
		private void MyPointMonitor(object sender, PointMonitorEventArgs e)
		{
			//получаемс список объектов под курсором
			FullSubentityPath[] fullEntPath = e.Context.GetPickedEntities();
			//проверяем, что под курсором что то есть
			if (fullEntPath.Length > 0)
			{
				//запускаем транзакцию
				Transaction transaction = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();

				try
				{
					//получаем верхний объект на чтение
					Entity ent = (Entity)transaction.GetObject(fullEntPath[0].GetObjectIds()[0], OpenMode.ForRead);
					//показываем всплывающее сообщение
					e.AppendToolTipText("эта штука есть " + ent.GetType().ToString());
					//т к работает с палитрой, проверяем её наличие
					if (myPalette == null)
					{
						return;
					}
					//создаём шрифты для палитры
					System.Drawing.Font fontRegular = new System.Drawing.Font("Microsoft Sans Serif", 8, System.Drawing.FontStyle.Regular);
					System.Drawing.Font fontBold = new System.Drawing.Font("Microsoft Sans Serif", 8, System.Drawing.FontStyle.Bold);
					//останавливаем обновление узлов дерево, вроде
					myPalette.treeView1.SuspendLayout();
					//ищем совпадение в дереве с объектом под курсором
					foreach (TreeNode node in myPalette.treeView1.Nodes)
					{
						if ((string)node.Tag == ent.ObjectId.ToString())
						{
							//если нашли совпадение, делаем текст жирным
							if (!fontBold.Equals(node.NodeFont))
							{
								node.NodeFont = fontBold;
								node.Text = node.Text;
							}
						}
						else
						{
							//в несовпадающих узлах делаем обычный шрифт
							if (!fontRegular.Equals(node.NodeFont))
							{
								node.NodeFont = fontRegular;
							}
						}
					}
					//запускаем обновления узлов
					myPalette.treeView1.ResumeLayout();
					myPalette.treeView1.Refresh();
					myPalette.treeView1.Update();
					//подтверждаем транзакцию
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
		//создаём команду на создание геометрии на объекте под курсором
		[CommandMethod("newInput")]
		public void NewInput()
		{
			// start our input point Monitor    
			// get the editor object
			Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

			// now add the delegate to the events list
			ed.PointMonitor += new PointMonitorEventHandler(MyInputMonitor);
			//что то вроде включения насильно привязки
			ed.TurnForcedPickOn();
			//делаем зпрос пользователю на точку
			PromptPointOptions getPointOptions = new PromptPointOptions("клацни де нить : ");
			PromptPointResult getPointResult = ed.GetPoint(getPointOptions);
			//снимаем прослушиватель после клаца 
			ed.PointMonitor -= new PointMonitorEventHandler(MyInputMonitor);

		}

		public void MyInputMonitor(object sender, PointMonitorEventArgs e)
		{
			//проверяем, что под курсором что то есть
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
						//перебираем все строки записи в словаре
						foreach (TypedValue myTypedValue in myXrecord.Data)
						{
							//если запись типо Real то ок
							if ((DxfCode)myTypedValue.TypeCode == DxfCode.Real)
							{
								//возвращает точку из словаря относительно начала кривой в глобальных координатах
								Point3d point = ent.GetPointAtDist((double)myTypedValue.Value);
								//делаем происходящее независимым от масштаба, хз как
								Point2d pixels = e.Context.DrawContext.Viewport.GetNumPixelsInUnitSquare(point);

								double xDist = 10 / pixels.X;
								double yDist = 10 / pixels.Y;
								//бахаем круг в точку из словаря, с радиусом из словаря с поправкой на масштаб
								Circle circle = new Circle(point, Vector3d.ZAxis, xDist);
								//рисуем круг в виде аннотации
								e.Context.DrawContext.Geometry.Draw(circle);
								//создаём текст
								DBText text = new DBText();
								//что то типо сброса настроек текста в модели
								text.SetDatabaseDefaults();
								//ставим текст рядом с соответсвующим кругом на значение его радиуса
								text.Position = point + new Vector3d(xDist, yDist, 0);
								//делаем значеие текста равным записи в словаре
								text.TextString = myTypedValue.Value.ToString();
								//устанавливаем высоту текста
								text.Height = yDist;
								//ресуем текст так же аннотацией
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

		private class MyCircleJig : EntityJig
		{
			private Point3d centerPoint;
			private Double radius;

			private int currentInputValue;

			public int CurrentInput
			{
				get
				{
					return currentInputValue;
				}
				set
				{
					currentInputValue = value;
				}
			}

			public MyCircleJig(Entity ent) : base(ent)
			{

			}

			protected override SamplerStatus Sampler(JigPrompts prompts)
			{
				switch (currentInputValue)
				{
					case 0:
						Point3d oldPnt = centerPoint;

						PromptPointResult jigPromptResult = prompts.AcquirePoint("Бахни середину");

						if (jigPromptResult.Status == PromptStatus.OK)
						{
							centerPoint = jigPromptResult.Value;

							if (oldPnt.DistanceTo(centerPoint) < 0.01)
							{
								return SamplerStatus.NoChange;
							}
						}

						return SamplerStatus.OK;
						break;

					case 1:

						double oldRadius = radius;

						JigPromptDistanceOptions jigPromptDistanceOptions = new JigPromptDistanceOptions("Бахни радиус ");

						jigPromptDistanceOptions.UseBasePoint = true;

						jigPromptDistanceOptions.BasePoint = centerPoint;

						PromptDoubleResult jigPromptPointResult = prompts.AcquireDistance(jigPromptDistanceOptions);

						if (jigPromptPointResult.Status == PromptStatus.OK)
						{
							radius = jigPromptPointResult.Value;

							if (System.Math.Abs(radius) < 0.1)
							{
								radius = 1;
							}

							if (System.Math.Abs(oldRadius - radius) < 0.01)
							{
								return SamplerStatus.NoChange;
							}
						}
						return SamplerStatus.OK;

						break;
				}
				return SamplerStatus.NoChange;

				//throw new NotImplementedException();
			}

			protected override bool Update()
			{
				switch (currentInputValue)
				{
					case 0:
						((Circle)this.Entity).Center = centerPoint;

						break;

					case 1:
						((Circle)this.Entity).Radius = radius;

						break;
				}
				return true;

				//throw new NotImplementedException();
			}
		}
		
		[CommandMethod("circleJig")]
		public void circleJig()
		{
			Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 10);

			MyCircleJig jig = new MyCircleJig(circle);

			for (int i = 0; i < 2; i++)
			{
				jig.CurrentInput = i;

				Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

				PromptResult promptResult = editor.Drag(jig);

				if ((promptResult.Status == PromptStatus.Cancel) || (promptResult.Status == PromptStatus.Error))
				{
					return;
				}
			}

			Database dwg = Application.DocumentManager.MdiActiveDocument.Database;
			// now start a transaction 
			Transaction trans = dwg.TransactionManager.StartTransaction();
			try
			{

				// open the current space for write 
				BlockTableRecord currentSpace = (BlockTableRecord)trans.GetObject(dwg.CurrentSpaceId, OpenMode.ForWrite);
				// add it to the current space 
				currentSpace.AppendEntity(circle);
				// tell the transaction manager about it 
				trans.AddNewlyCreatedDBObject(circle, true);

				// all ok, commit it 

				trans.Commit();
			}
			catch (Exception ex)
			{
				Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.ToString());
			}
			finally
			{
				// whatever happens we must dispose the transaction 

				trans.Dispose();

			}

		}
	}
}

