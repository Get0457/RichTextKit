namespace Get.RichTextKit.Editor.DataStructure.Table;

interface ITableOwner<T>
{
    ITable<T> Owner { get; }
}