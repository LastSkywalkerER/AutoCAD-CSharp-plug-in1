// (C) Copyright 2021 by  
//
using System;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Windows;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

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
            PromptKeywordOptions getWhichEntityOptions = new PromptKeywordOptions("Чё создаём? [Circle/Block] : " , "Circle Block");
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
				myPaletteSet.Add("SkyPalette1", myPalette);

			}
			//показываем набор палитр
			myPaletteSet.Visible = true;

		}

		
    }

}
