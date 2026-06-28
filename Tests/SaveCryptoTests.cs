using Xunit;

// M8: the save body is AES-encrypted (unreadable in a text editor) but round-trips exactly.
public class SaveCryptoTests
{
    [Fact]
    public void Encrypt_then_decrypt_round_trips()
    {
        string plain = "{\"Seed\":12345,\"Tick\":42,\"msg\":\"hello world\"}";
        string cipher = SaveCrypto.Encrypt(plain);
        Assert.Equal(plain, SaveCrypto.Decrypt(cipher));
    }

    [Fact]
    public void Ciphertext_is_not_readable_plaintext()
    {
        string plain = "treasury=999999 secret save data";
        string cipher = SaveCrypto.Encrypt(plain);
        Assert.NotEqual(plain, cipher);
        Assert.DoesNotContain("treasury", cipher);
        Assert.DoesNotContain("secret", cipher);
    }

    [Fact]
    public void Empty_string_round_trips()
    {
        Assert.Equal("", SaveCrypto.Decrypt(SaveCrypto.Encrypt("")));
    }
}
