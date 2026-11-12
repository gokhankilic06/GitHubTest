import { createContext, useContext, useState, type ReactNode } from 'react';
import { apiClient } from '../api/client';

interface Kullanici {
  kullaniciAdi: string;
  adSoyad: string;
}

interface AuthBaglami {
  kullanici: Kullanici | null;
  girisYap: (kullaniciAdi: string, sifre: string) => Promise<void>;
  cikisYap: () => void;
}

const AuthContext = createContext<AuthBaglami | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [kullanici, setKullanici] = useState<Kullanici | null>(() => {
    const kullaniciAdi = localStorage.getItem('kullaniciAdi');
    const adSoyad = localStorage.getItem('adSoyad');
    return kullaniciAdi && adSoyad ? { kullaniciAdi, adSoyad } : null;
  });

  async function girisYap(kullaniciAdi: string, sifre: string) {
    const res = await apiClient.post('/api/auth/giris', { kullaniciAdi, sifre });
    const { token, kullaniciAdi: donenAd, adSoyad } = res.data;
    localStorage.setItem('token', token);
    localStorage.setItem('kullaniciAdi', donenAd);
    localStorage.setItem('adSoyad', adSoyad);
    setKullanici({ kullaniciAdi: donenAd, adSoyad });
  }

  function cikisYap() {
    localStorage.removeItem('token');
    localStorage.removeItem('kullaniciAdi');
    localStorage.removeItem('adSoyad');
    setKullanici(null);
  }

  return (
    <AuthContext.Provider value={{ kullanici, girisYap, cikisYap }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth, AuthProvider içinde kullanılmalıdır.');
  return ctx;
}
