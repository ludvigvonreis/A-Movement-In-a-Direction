public struct OnUpdateAmmo : IMessage
{
	public int CurrentAmmo;
	public int AmmoReserves;
}

public struct OnUpdateDashTimeout : IMessage
{
	public float Timeout;
}