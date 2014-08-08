using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;

namespace TransferPngToSplitter
{
	class Program
	{
		public static object LockObjectDocumentAttributes = new Object();
		public static object LockObjectCount = new Object();
		public static object HowManyLock = new Object();

		public static int howMany = 0;
		static void Main(string[] args)
		{
			var inPathBase = @"\\londat02\irooms development\@ApplicationSupport\TEMP\R2405\";
			var outPathBase = @"\\londat02\irooms development\@ApplicationSupport\TEMP\ForSplitter\";
			//var outPathBase = @"c:/temp/out/";

			Directory.CreateDirectory(outPathBase);

			foreach(var inPath in Directory.GetDirectories(inPathBase, "*_process"))
			{
				Console.WriteLine(inPath);
				
				var count = Directory.GetFiles(inPath,"*.png").Count();

				Console.WriteLine("{0} files to process...\npress a key to start.", count);
				//Console.ReadKey();
				var files = Directory.GetFiles(inPath,"*.png");
				var result = Parallel.ForEach(files,new ParallelOptions() { MaxDegreeOfParallelism = 10},  f => 
				{
					var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(f);
					var parts = fileNameWithoutExtension.Split('_');
					var roomId = Int32.Parse(parts[0]);
					var documentId = Int32.Parse(parts[1]);
					var folderSubfolder = GetFolderSubfolder(documentId);
					var outPath = Path.Combine(outPathBase, String.Format("R{0}", roomId));
					outPath = Path.Combine(outPath, folderSubfolder);
					Directory.CreateDirectory(outPath);

					var published = Int32.Parse(parts[2]);
					var pageNumber = 1;
					if (parts.Length >= 5) // there is more than one page get it 
					{
						pageNumber = Int32.Parse(parts[4]);
					}
					var outputPngName = String.Format("{0}_{1}_{2}.pdf_{3}.png", roomId, documentId, published, pageNumber);
					var outputAttributeName = String.Format("{0}_{1}_{2}.pdf_{3}_PageAttributes.txt", roomId, documentId, published, pageNumber);
					var outputDocumentName = String.Format("{0}_{1}_{2}.pdf_DocumentAttributes.txt", roomId, documentId, published);

					var documentAttributesPath = Path.Combine(outPath, outputDocumentName);


					using (var image = Image.FromFile(f))
					{
						var width = image.Size.Width;
						var height = image.Size.Height;
						var format = image.PixelFormat;
						if (format != PixelFormat.Format24bppRgb && format != PixelFormat.Format1bppIndexed && format != PixelFormat.Format32bppArgb)
						{
							System.Diagnostics.Debugger.Break();
						}
						
						

						var xResolution = 0;
						var yResolution = 0;
						var attributeText = String.Format("{{\"version\":\"8.3\",\"contentType\":\"jpeg,png\",\"imageBitDepth\":{0},\"imageHeight\":{1},\"imageWidth\":{2},\"imageXResolution\":{3},\"imageYResolution\":{4}}}",
							8, height, width, xResolution, yResolution);

						File.WriteAllText(Path.Combine(outPath, outputAttributeName), attributeText);
						var pngOutputWithPath = Path.Combine(outPath, outputPngName);
						if (format == PixelFormat.Format1bppIndexed)
						{
							try
							{

								/*
								Bitmap clone = new Bitmap(orig.Width, orig.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
								using (Graphics gr = Graphics.FromImage(clone)) {
									gr.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
								}
								*/


								using(var image2 = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb))
								{
									using (Graphics gr = Graphics.FromImage(image2)) {
										gr.DrawImage(image, new Rectangle(0, 0, image2.Width, image2.Height));
									}
									image2.Save(pngOutputWithPath);
									lock(HowManyLock) 
									{
										howMany++;
										File.AppendAllText("c:/temp/formatchanged.txt",f + "\n");
									}
								}
							}
							catch(Exception excp) {
								File.AppendAllText("c:/temp/errorfiles.txt",f);
							}
							
						}
						else 
						{
							File.Copy(f, pngOutputWithPath, true);
						}

						lock (LockObjectDocumentAttributes)
						{
							if (!File.Exists(documentAttributesPath))
							{
								var documentAttributesTemplate = "version\":\"8.3\",\"pageCountConfidence\":100,\"documentFormat\":\"pdf\",\"documentContentType\":\"jpeg,png\",\"pageCount\":{0}";
								var documentAttributeContent = String.Format(documentAttributesTemplate, 0);
								File.WriteAllText(documentAttributesPath, "{" + documentAttributeContent + "}");
							}

							var docuemtnAttributesContent = File.ReadAllText(documentAttributesPath);
							var pattern = @":(\d+)}";
							var r = new Regex(pattern);
							var match = r.Match(docuemtnAttributesContent);

							var previousPageCount = Int32.Parse(match.Groups[1].Value);
							var newPageCount = previousPageCount + 1;
							var newDocumentContent = r.Replace(docuemtnAttributesContent, ":" + newPageCount.ToString() + "}");
							File.WriteAllText(documentAttributesPath, newDocumentContent);
						}


					}
					lock (LockObjectCount)
					{
						
						count--;
						if (count % 10 == 0)
						{
							Console.WriteLine("still {0} to go", count);
						}
					}
				});

				
			
			
			
			
			}
			/*
			foreach(var f in Directory.GetFiles(inPath,"*.png"))
			{
				var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(f);
				var parts = fileNameWithoutExtension.Split('_');
				var roomId = Int32.Parse(parts[0]);
				var documentId = Int32.Parse(parts[1]);
				var folderSubfolder = GetFolderSubfolder(documentId);
				var outPath = Path.Combine(outPathBase, String.Format("R{0}", roomId));
				outPath = Path.Combine(outPath, folderSubfolder);
				Directory.CreateDirectory(outPath);

				var published = Int32.Parse(parts[2]);
				var pageNumber = 1;
				if( parts.Length >= 5 ) // there is more than one page get it 
				{
					pageNumber = Int32.Parse(parts[4]);
				}
				var outputPngName = String.Format("{0}_{1}_{2}.pdf_{3}.png", roomId,documentId,published,pageNumber);
				var outputAttributeName = String.Format("{0}_{1}_{2}.pdf_{3}_PageAttributes.txt", roomId,documentId,published,pageNumber);
				var outputDocumentName = String.Format("{0}_{1}_{2}.pdf_DocumentAttributes.txt", roomId,documentId,published);
				
				var documentAttributesPath = Path.Combine(outPath, outputDocumentName);
				if(!File.Exists(documentAttributesPath))
				{
					var documentAttributesTemplate = "version\":\"8.3\",\"pageCountConfidence\":100,\"documentFormat\":\"pdf\",\"documentContentType\":\"jpeg,png\",\"pageCount\":{0}";
					var documentAttributeContent = String.Format(documentAttributesTemplate,0);
					File.WriteAllText(documentAttributesPath,"{" + documentAttributeContent + "}");
				}

				using(var image = Image.FromFile( f ))
				{
					var width  =image.Size.Width;
					var height = image.Size.Height;
					var format = image.PixelFormat;
					if (format != PixelFormat.Format24bppRgb && format != PixelFormat.Format1bppIndexed && format != PixelFormat.Format32bppArgb)
					{
						System.Diagnostics.Debugger.Break();
					}
					var xResolution = 0;
					var yResolution = 0;
					var attributeText = String.Format("{{\"version\":\"8.3\",\"contentType\":\"jpeg,png\",\"imageBitDepth\":{0},\"imageHeight\":{1},\"imageWidth\":{2},\"imageXResolution\":{3},\"imageYResolution\":{4}}}",
						8, height, width, xResolution, yResolution);

					File.WriteAllText(Path.Combine(outPath, outputAttributeName), attributeText);
					var pngOutputWithPath = Path.Combine(outPath, outputPngName);
					File.Copy(f, pngOutputWithPath,true);

					lock(LockObjectDocumentAttributes)
					{
						var docuemtnAttributesContent = File.ReadAllText(documentAttributesPath);
						var pattern = @":(\d+)}";
						var r = new Regex(pattern);
						var match = r.Match(docuemtnAttributesContent);

						var previousPageCount =Int32.Parse( match.Groups[1].Value);
						var newPageCount = previousPageCount+1;
						var newDocumentContent = r.Replace(docuemtnAttributesContent, ":" + newPageCount.ToString() + "}");
						File.WriteAllText(documentAttributesPath, newDocumentContent);
					}


				}
				lock(LockObjectCount)
				{
					count--;
					if(count%10 == 0)
					{
						Console.WriteLine("still {0} to go", count);
					}
				}

			}
			*/
			Console.WriteLine("finished");
			
			Console.ReadKey();
			
		}

		private static string GetFolderSubfolder(int documentId)
		{
			var docIdAsString = documentId.ToString();
			var idLength = docIdAsString.Length;
			var subfolder = docIdAsString.Substring(idLength-3,3);
			var folder = docIdAsString.Substring(0, idLength-3);
			
			return Path.Combine(folder,subfolder);
		}
	}
}
