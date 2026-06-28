using System;
using System.Security.Cryptography;
using System.Text;

// M8: symmetric AES encryption for the save body so the file is unreadable in a text editor. Pure .NET (no
// Godot) → unit-testable. The key is derived (PBKDF2) from a constant passphrase baked into the assembly;
// that's the standard trade-off for a single-player game — the goal is to deter casual save editing, not to
// resist a determined attacker, and the assembly itself is obfuscated at ship (see Obfuscar.xml). The world
// SEED is deliberately kept OUT of here: Serialization stores it in a plain header so saves stay shareable.
public static class SaveCrypto
{
    private const string Passphrase = "vigilant-broccoli-save-v1";
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("vbsalt-2026-econ");

    // Deterministic key+IV from the passphrase so any build reads any save. (Deterministic IV is acceptable
    // here: this is obfuscation of single-player saves, not a multi-message secure channel.)
    private static Aes CreateAes()
    {
        Aes aes = Aes.Create();
        using var kdf = new Rfc2898DeriveBytes(Passphrase, Salt, 100_000, HashAlgorithmName.SHA256);
        aes.Key = kdf.GetBytes(32); // AES-256
        aes.IV = kdf.GetBytes(16);
        return aes;
    }

    // Plaintext (JSON) -> base64 ciphertext (single line, no newlines).
    public static string Encrypt(string plaintext)
    {
        using Aes aes = CreateAes();
        using ICryptoTransform enc = aes.CreateEncryptor();
        byte[] data = Encoding.UTF8.GetBytes(plaintext);
        byte[] cipher = enc.TransformFinalBlock(data, 0, data.Length);
        return Convert.ToBase64String(cipher);
    }

    // Base64 ciphertext -> plaintext (JSON).
    public static string Decrypt(string base64Cipher)
    {
        using Aes aes = CreateAes();
        using ICryptoTransform dec = aes.CreateDecryptor();
        byte[] cipher = Convert.FromBase64String(base64Cipher);
        byte[] data = dec.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(data);
    }
}
