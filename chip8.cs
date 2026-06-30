using System;
using System.IO;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using Raylib_cs;

namespace Emulator
{
    internal class Program
    {
        public static class Chip8Sound
{
    private static bool isPlaying = false;
    private static CancellationTokenSource cts;

    public static void Play()
    {
        if (isPlaying) return; 
        
        isPlaying = true;
        cts = new CancellationTokenSource();
        var token = cts.Token;

        Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                System.Console.Beep(400, 50); 
            }
        }, token);
    }

    public static void Stop()
    {
        if (!isPlaying) return;
        
        cts?.Cancel();
        isPlaying = false;
    }
        static void Main(string[] args)
        {
            {
                // declaring
                byte[] memory = new byte[4096];
                int pc = 0x200;
                int I = 0;
                byte[] V = new byte[16];
                bool[,] display = new bool[64, 32];
                byte[] romBytes = File.ReadAllBytes("Animal Race [Brian Astle].ch8");
                ushort[] stack = new ushort[16];
                int sp = 0;
                int delayTimer = 0;
                int soundTimer = 0;
                bool[] keys = new bool[16];

                byte[] font =
{
    0xF0,0x90,0x90,0x90,0xF0, //0
    0x20,0x60,0x20,0x20,0x70, //1
    0xF0,0x10,0xF0,0x80,0xF0, //2
    0xF0,0x10,0xF0,0x10,0xF0, //3
    0x90,0x90,0xF0,0x10,0x10, //4
    0xF0,0x80,0xF0,0x10,0xF0, //5
    0xF0,0x80,0xF0,0x90,0xF0, //6
    0xF0,0x10,0x20,0x40,0x40, //7
    0xF0,0x90,0xF0,0x90,0xF0, //8
    0xF0,0x90,0xF0,0x10,0xF0, //9
    0xF0,0x90,0xF0,0x90,0x90, //A
    0xE0,0x90,0xE0,0x90,0xE0, //B
    0xF0,0x80,0x80,0x80,0xF0, //C
    0xE0,0x90,0x90,0x90,0xE0, //D
    0xF0,0x80,0xF0,0x80,0xF0, //E
    0xF0,0x80,0xF0,0x80,0x80  //F
};

                // Load Font
                for(int i = 0; i < font.Length; i++)
                {
                    memory[0x50 + i] = font[i];
                }

                Raylib.InitWindow(64 * 10, 32 * 10, "CHIP-8 Emulator");
                Raylib.SetTargetFPS(60);

                for(int i = 0; i < romBytes.Length; i++)
                {
                    memory[pc + i] = romBytes[i];
                }

                while (!Raylib.WindowShouldClose())
                {
                    // Keys
                        keys[0x1] = Raylib.IsKeyDown(KeyboardKey.One);
                        keys[0x2] = Raylib.IsKeyDown(KeyboardKey.Two);
                        keys[0x3] = Raylib.IsKeyDown(KeyboardKey.Three);
                        keys[0xC] = Raylib.IsKeyDown(KeyboardKey.Four);
                        keys[0x4] = Raylib.IsKeyDown(KeyboardKey.Q);
                        keys[0x5] = Raylib.IsKeyDown(KeyboardKey.W);
                        keys[0x6] = Raylib.IsKeyDown(KeyboardKey.E);
                        keys[0xD] = Raylib.IsKeyDown(KeyboardKey.R);
                        keys[0x7] = Raylib.IsKeyDown(KeyboardKey.A);
                        keys[0x8] = Raylib.IsKeyDown(KeyboardKey.S);
                        keys[0x9] = Raylib.IsKeyDown(KeyboardKey.D);
                        keys[0xE] = Raylib.IsKeyDown(KeyboardKey.F);
                        keys[0xA] = Raylib.IsKeyDown(KeyboardKey.Z);
                        keys[0x0] = Raylib.IsKeyDown(KeyboardKey.X);
                        keys[0xB] = Raylib.IsKeyDown(KeyboardKey.C);
                        keys[0xF] = Raylib.IsKeyDown(KeyboardKey.V);
                    for (int i = 0; i < 10; i++){
                    ushort opcode = (ushort)(memory[pc] << 8 | memory[pc + 1]);
                    
                    
                    int x = (opcode & 0x0F00) >> 8;
                    int y = (opcode & 0x00F0) >> 4;
                    int nn = (opcode & 0x00FF);
                    int nnn = (opcode & 0x0FFF);
                    int N = opcode & 0x000F;

                    if((opcode & 0xF000) == 0x6000)
                    {
                        V[x] = (byte)nn;
                    }
                    else if((opcode & 0xF000) == 0xA000)
                    {
                        I = nnn;
                    }
                    else if((opcode & 0xF000) == 0xD000)
                    {
                        // graphics
                        int screenX = V[x];
                        int screenY = V[y];
                        V[0xF] = 0;

                        for(int row=0; row < N; row++)
                        {
                            byte spriteRow = (byte)memory[row + I];
                            for(int col=0; col < 8; col++)
                            {
                                bool pixel = (spriteRow & (0x80 >> col)) != 0;
                                if(pixel)
                                {
                                    if (screenX + col < 64 && screenY + row < 32)
                                    {
                                        int px = (screenX + col) % 64;
                                        int py = (screenY + row) % 32;

                                        if (display[px, py])
                                            V[0xF] = 1;

                                        display[px, py] ^= true;
                                    }
                                }
                            }
                        }
                        
                    }
                    else if((opcode & 0xF000) == 0x1000)
                    {
                        pc = nnn;
                        continue;
                    }
                    else if((opcode & 0xF000) == 0x7000)
                    {
                        V[x] += (byte)nn;
                    }
                    else if((opcode & 0xF000) == 0x3000)
                    {
                        if(V[x] == nn)
                        {
                            pc+=2;
                        }
                    }
                    else if((opcode & 0xF000) == 0x4000)
                    {
                        if(V[x] != nn)
                        {
                            pc+=2;
                        }
                    }
                    else if((opcode & 0xF00F) == 0x5000)
                    {
                        if(V[x] == V[y])
                        {
                            pc+=2;
                        }
                    }
                    else if((opcode & 0xF00F) == 0x9000)
                    {
                        if(V[x] != V[y])
                        {
                            pc+=2;
                        }
                    }
                    else if((opcode & 0xF00F) == 0x8000)
                    {
                        V[x] = V[y];
                    }
                    else if((opcode & 0xF00F) == 0x8001)
                    {
                        V[x] = (byte)(V[x] | V[y]);
                    }
                    else if((opcode & 0xF00F) == 0x8002)
                    {
                        V[x] = (byte)(V[x] & V[y]);
                    }
                    else if((opcode & 0xF00F) == 0x8003)
                    {
                        V[x] = (byte)(V[x] ^ V[y]);
                    }
                    else if((opcode & 0xF00F) == 0x8004)
                    {
                        int sum = V[x] + V[y];

                        V[0xF] = (byte)(sum > 255 ? 1 : 0);
                        V[x] = (byte)sum;
                    }
                    else if((opcode & 0xF00F) == 0x8005)
                    {
                        V[0xF] = (byte)(V[x] >= V[y] ? 1 : 0);
                        V[x] = (byte)(V[x] - V[y]);
                    }
                    else if((opcode & 0xF00F) == 0x8006)
                    {
                        V[0xF] = (byte)(V[x] & 0x01);
                        V[x] >>= 1;
                    }
                    else if((opcode & 0xF00F) == 0x8007)
                    {
                        V[0xF] = (byte)(V[y] >= V[x] ? 1 : 0);
                        V[x] = (byte)(V[y] - V[x]);
                    }
                    else if((opcode & 0xF00F) == 0x800E)
                    {
                        V[0xF] = (byte)((V[x] & 0x80) >> 7);
                        V[x] <<= 1;
                    }
                    else if((opcode & 0xF0FF) == 0xF007)
                    {
                        V[x] = (byte)(delayTimer);
                    }
                    else if((opcode & 0xF0FF) == 0xF015)
                    {
                        delayTimer = V[x];
                    }
                    else if((opcode & 0xF0FF) == 0xF018)
                    {
                        soundTimer = V[x];
                    }
                    else if(opcode == 0x00E0)
                    {
                        for(int e = 0; e < 32; e++)
                        {
                            for(int d = 0; d < 64; d++)
                            {
                                display[d, e] = false;
                            }
                        }
                    }
                    else if((opcode & 0xF000) == 0x2000)
                    {
                        stack[sp] = (ushort)pc;
                        sp++;
                        pc = nnn;
                        continue;
                    }
                    else if (opcode == 0x00EE)
                    {
                        sp--;
                        pc = stack[sp];
                        pc+=2;
                        continue;
                    }
                    else if((opcode & 0xF000) == 0xC000)
                    {
                        int randomNumber = Random.Shared.Next(0, 256); 
                        V[x] = (byte)(randomNumber & nn);
                    }
                    else if((opcode & 0xF000) == 0xB000)
                    {
                        pc = nnn + V[0];
                        continue;
                    }
                    else if ((opcode & 0xF0FF) == 0xF01E)
                    {
                        I += V[x];
                        if(I > 0x0FFF){
                            V[0xF] = 1;
                        }
                        else{
                            V[0xF] = 0;
                        }
                    }
                    else if ((opcode & 0xF0FF) == 0xF033)
                    {
                        memory[I]     = (byte)(V[x] / 100);
                        memory[I + 1] = (byte)((V[x] / 10) % 10);
                        memory[I + 2] = (byte)(V[x] % 10);
                    }
                    else if((opcode & 0xF0FF) == 0xE09E)
                    {
                        if(keys[V[x]])
                        {
                            pc+=2;
                        }
                    }
                    else if((opcode & 0xF0FF) == 0xE0A1)
                    {
                        if(!keys[V[x]])
                        {
                            pc+=2;
                        }
                    }
                    else if((opcode & 0xF0FF) == 0xF00A)
                    {
                        bool keyPressed = false;

                        for(int k = 0; k < 16; k++)
                        {
                            if(keys[k])
                            {
                                V[x] = (byte)k;
                                keyPressed = true;
                                break;
                            }
                        }
                        if(!keyPressed)
                        {
                            pc -= 2;
                        }
                    }
                    else if ((opcode & 0xF0FF) == 0xF029)
                    {
                        I = 0x50 + (V[x] * 5);
                    }
                    else if ((opcode & 0xF0FF) == 0xF055)
                    {
                        for(int z = 0; z <= x; z++)
                        {
                            memory[I + z] = V[z];
                        }
                    }
                    else if ((opcode & 0xF0FF) == 0xF065)
                    {
                        for(int c = 0; c <= x; c++)
                        {
                            V[c] = memory[I + c];
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Unknown opcode: {opcode:X4}");
                    }
                    pc+=2;
                    }
                    Raylib.BeginDrawing();

                        Raylib.ClearBackground(Color.Black);

                        for (int yPos = 0; yPos < 32; yPos++)
                        {   
                            for (int xPos = 0; xPos < 64; xPos++)
                            {
                                if (display[xPos, yPos])
                                {
                                    Raylib.DrawRectangle(
                                    xPos * 10,
                                    yPos * 10,
                                    10,
                                    10,
                                    Color.White
                                    );
                                }
                            }
                        }

                    Raylib.EndDrawing();
                       if(delayTimer > 0)
                       {
                        delayTimer--;
                       }

                       if(soundTimer > 0)
                       {
                        Chip8Sound.Play();
                        soundTimer--;
                       }
                       else
                       {
                         Chip8Sound.Stop();
                       }
                       
                }

            }
        }
    }
}}
