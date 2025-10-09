public struct OnUpdateAmmo : IMessage
{
	public int CurrentAmmo;
	public int AmmoReserves;
}

public struct OnUpdateStamina : IMessage
{
	public float Stamina;
	public float MaxStamina;
}