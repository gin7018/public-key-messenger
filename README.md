# public-key-messenger
messaging system that uses public key encryption (RSA)

- This is a distributed system where keys are stored on a server 
- This messaging system implements the RSA encryption and decryption algorithm to send secure messages to other users.
- It also uses the prime number generator to generate large prime numbers (1024+ bits) used for the private and public RSA keys

### usage
```
dotnet run <option> <other arguments> 
       genKey    - keySize
       sendKey   - email 
       getKey    - email 
       sendMsg   - email plaintext 
       getMsg    - email
```
