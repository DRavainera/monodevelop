using System;
using System.IO;
using System.Collections.Generic;

class SortFix2 {
    static int IW(int rc) => rc >= 0x10000 ? 4 : 2;
    static int Coded(int[] tabRc) { int mx = 0; foreach (int r in tabRc) if (r > mx) mx = r; return mx >= (1 << 14) ? 4 : 2; }
    static int Main(string[] args) {
        string path = args[0];
        byte[] b = File.ReadAllBytes(path);
        if (b.Length < 0x40 || System.Text.Encoding.ASCII.GetString(b, 0, 2) != "MZ") { Console.Error.WriteLine("not PE"); return 1; }
        int peOff = BitConverter.ToInt32(b, 0x3C);
        int optMagic = BitConverter.ToInt16(b, peOff + 24);
        int optSize = BitConverter.ToInt16(b, peOff + 20);
        bool pe32p = optMagic == 0x20b;
        int dataDirBase = peOff + 24 + (pe32p ? 112 : 96);
        int cliRva = BitConverter.ToInt32(b, dataDirBase + 14 * 8);
        int nsect = BitConverter.ToInt16(b, peOff + 6);
        int secTab = peOff + 24 + optSize;
        int metaRva = 0;
        for (int s = 0; s < nsect; s++) {
            int st = secTab + s * 40;
            int va = BitConverter.ToInt32(b, st + 12), vsz = BitConverter.ToInt32(b, st + 8);
            int raw = BitConverter.ToInt32(b, st + 20);
            if (cliRva >= va && cliRva < va + vsz) { int cliFoff = raw + (cliRva - va); metaRva = BitConverter.ToInt32(b, cliFoff + 8); }
        }
        int fileOff = -1;
        for (int s = 0; s < nsect; s++) {
            int st = secTab + s * 40;
            int va = BitConverter.ToInt32(b, st + 12), vsz = BitConverter.ToInt32(b, st + 8);
            int raw = BitConverter.ToInt32(b, st + 20);
            if (metaRva >= va && metaRva < va + vsz) { fileOff = raw + (metaRva - va); break; }
        }
        if (BitConverter.ToInt32(b, fileOff) != 0x424A5342) { Console.Error.WriteLine("no BSJB"); return 5; }
        int verLen = BitConverter.ToInt32(b, fileOff + 12);
        int sp = fileOff + 16 + verLen;
        int nstreams = BitConverter.ToInt16(b, sp + 2); sp += 4;
        int hash = 0;
        for (int i = 0; i < nstreams; i++) {
            int soff = BitConverter.ToInt32(b, sp), szz = BitConverter.ToInt32(b, sp + 4);
            int np = sp + 8; while (b[np] != 0) np++;
            string name = System.Text.Encoding.ASCII.GetString(b, sp + 8, np - (sp + 8));
            if (name == "#~") hash = fileOff + soff;
            sp = sp + 8 + (((np - (sp + 8)) + 1 + 3) & ~3);
        }
        int p = hash;
        int heapSizes = b[p + 6];
        bool strW = (heapSizes & 1) != 0, guidW = (heapSizes & 2) != 0, blobW = (heapSizes & 4) != 0;
        long valid = BitConverter.ToInt64(b, p + 8);
        long sorted = BitConverter.ToInt64(b, p + 16);
        var present = new List<int>();
        for (int t = 0; t < 64; t++) if ((valid & (1L << t)) != 0) present.Add(t);
        // RowCounts are packed: one 4-byte count per present table, ascending
        var rc = new int[64];
        int rpos = p + 24;
        foreach (int t in present) { rc[t] = BitConverter.ToInt32(b, rpos); rpos += 4; }
        int strS = strW ? 4 : 2, guidS = guidW ? 4 : 2, blobS = blobW ? 4 : 2;
        int typescope = Coded(new[] { rc[0], rc[26], rc[23], rc[1] });
        int tdor = Coded(new[] { rc[2], rc[1], rc[27] });
        int tomd = Coded(new[] { rc[2], rc[6] });
        int[] sz = new int[64];
        sz[0] = 2 + strS + guidS + guidS + guidS;
        sz[1] = typescope + strS + strS;
        sz[2] = 4 + strS + strS + tdor + IW(rc[4]) + IW(rc[6]);
        sz[3] = IW(rc[4]);
        sz[4] = 2 + strS + blobS;
        sz[5] = IW(rc[6]);
        sz[6] = 4 + 2 + 2 + strS + blobS + IW(rc[8]);
        sz[7] = IW(rc[8]);
        sz[8] = 2 + 2 + strS;
        sz[9] = IW(rc[2]) + tdor;                       // InterfaceImpl
        sz[10] = Coded(new[] { rc[6], rc[1], rc[2], rc[26], rc[27] }) + IW(rc[40]) + strS + blobS;
        sz[11] = 2 + 2 + Coded(new[] { rc[4], rc[8], rc[21] }) + blobS;
        sz[12] = IW(rc[2]) + Coded(new[] { rc[6], rc[4], rc[1], rc[2], rc[0], rc[8], rc[21], rc[22], rc[17], rc[16] }) + Coded(new[] { rc[6], rc[10], rc[43] }) + blobS;
        sz[13] = Coded(new[] { rc[4], rc[8] }) + blobS;
        sz[14] = 2 + 2 + Coded(new[] { rc[2], rc[6], rc[32] }) + blobS;
        sz[17] = blobS;
        sz[18] = IW(rc[2]) + IW(rc[19]);
        sz[20] = IW(rc[2]) + IW(rc[21]);
        sz[21] = 2 + IW(rc[19]) + strS + blobS;
        sz[22] = 2 + 4 + Coded(new[] { rc[8], rc[21] });
        sz[23] = IW(rc[2]) + Coded(new[] { rc[6], rc[10] }) + Coded(new[] { rc[6], rc[10] });
        sz[24] = 2 + IW(rc[2]) + Coded(new[] { rc[4], rc[6] }) + strS;
        sz[25] = IW(rc[4]) + 4;
        sz[26] = strS;
        sz[27] = blobS;
        sz[32] = 4 + 2 + 2 + strS + blobS + 2;          // Assembly (approx; not reached)
        sz[35] = 4 + strS + blobS;
        sz[41] = IW(rc[2]) + IW(rc[2]);
        sz[43] = IW(rc[2]) + Coded(new[] { rc[6], rc[10] }) + blobS;
        // compute offsets
        var tabs = new int[64];
        int pos = rpos;
        foreach (int t in present) { tabs[t] = pos; pos += sz[t] * rc[t]; }
        int rc9 = rc[9];
        int row = sz[9];
        int baseOff = tabs[9];
        Console.WriteLine("DEBUG rc9=" + rc9 + " row=" + row + " off9=" + baseOff + " sortedBefore=" + sorted);
        // read rows: Class (TypeDef idx) + Interface (TypeDefOrRef coded)
        var cls = new int[rc9]; var ifv = new int[rc9]; var orig = new int[rc9];
        for (int i = 0; i < rc9; i++) {
            int r = baseOff + i * row;
            int cw = IW(rc[2]);
            cls[i] = cw == 4 ? BitConverter.ToInt32(b, r) : (b[r] | (b[r + 1] << 8));
            int ir = r + cw;
            int iw_ = row - cw;
            ifv[i] = iw_ == 4 ? BitConverter.ToInt32(b, ir) : (b[ir] | (b[ir + 1] << 8));
            orig[i] = i;
        }
        Array.Sort(orig, (a, c) => { int d = cls[a].CompareTo(cls[c]); return d != 0 ? d : ifv[a].CompareTo(ifv[c]); });
        byte[] tmp = new byte[rc9 * row];
        int tp = 0;
        foreach (int i in orig) { Buffer.BlockCopy(b, baseOff + i * row, tmp, tp, row); tp += row; }
        Buffer.BlockCopy(tmp, 0, b, baseOff, tmp.Length);
        sorted |= (1L << 9);
        Buffer.BlockCopy(BitConverter.GetBytes(sorted), 0, b, p + 16, 8);
        File.WriteAllBytes(path + ".fix", b);
        Console.WriteLine("OK sorted " + rc9 + " rows");
        return 0;
    }
}
