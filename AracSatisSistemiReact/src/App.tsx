import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { AnaLayout } from './layouts/AnaLayout';
import { Giris } from './pages/Giris';
import { AnaSayfa } from './pages/AnaSayfa';
import { Yakinda } from './pages/Yakinda';
import { DosyaListele } from './pages/dosya/DosyaListele';
import { DosyaOlustur } from './pages/dosya/DosyaOlustur';
import { DosyaDetay } from './pages/dosya/DosyaDetay';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/giris" element={<Giris />} />
          <Route
            element={
              <ProtectedRoute>
                <AnaLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/" element={<AnaSayfa />} />
            <Route path="/dosya" element={<DosyaListele />} />
            <Route path="/dosya/yeni" element={<DosyaOlustur />} />
            <Route path="/dosya/:id" element={<DosyaDetay />} />
            <Route path="/yakinda" element={<Yakinda />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
