namespace Cimon.DB;

public interface IEntityCreator<out T>
{
	static abstract T Create();
}
