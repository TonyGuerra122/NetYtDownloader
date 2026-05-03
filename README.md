# 🎬 NetYtDownloader

Um aplicativo desktop moderno desenvolvido em **WPF (.NET)** para buscar e baixar vídeos do YouTube com uma interface elegante e responsiva.

---

## ✨ Funcionalidades

- 🔎 Buscar vídeos diretamente do YouTube
- ▶️ Abrir vídeos no navegador
- ⬇️ Baixar vídeos em alta qualidade
- 🎧 Suporte a streams separados (áudio + vídeo via FFmpeg)
- ⚡ Interface rápida e fluida
- 🌙 Tema escuro moderno
- ⌨️ Atalho de teclado (Enter para pesquisar)
- 🌀 Loader com spinner durante operações
- 📁 Abertura automática da pasta de downloads

---

## 🖥️ Tecnologias utilizadas

- **WPF (.NET 8/9/10)**
- **C#**
- **YoutubeExplode**
- **FFmpeg**
- **MVVM Pattern**

---

## 📦 Estrutura do Projeto
- GUI/ → Interface WPF
- YtLibrary/ → Lógica de busca/download do YouTube
- FFmpegLibrary/ → Gerenciamento do FFmpeg


---

## ⚙️ Dependências

O programa utiliza:

- [`YoutubeExplode`](https://github.com/Tyrrrz/YoutubeExplode)
- `FFmpeg` (baixado automaticamente pelo app)

---

## 🚀 Como executar

### 1. Clonar o projeto

```bash
git clone https://github.com/seu-usuario/NetYtDownloader.git
cd NetYtDownloader
```

### 2. Restaurar pacotes
```bash
dotnet restore
```

### 3. Executar
```bash
dotnet run --project GUI
```

---

## 🔧 FFmpeg
- O FFmpeg é baixado automaticamente na primeira execução
- Local padrão: `C:\Users\<SeuUsuario>\Videos\NetYtDownloader\FFmpeg`

---

## 📁 Downloads
Os vídeos são salvos em: `C:\Users\<SeuUsuario>\Videos\NetYtDownloader`

---

## 🎨 Interface
- UI moderna com tema escuro
- Cards animados
- Scroll customizado
- Loader com overlay e spinner

---

## ⚠️ Observações
- É necessário conexão com internet
- O download depende da disponibilidade do vídeo no YouTube
- FFmpeg é obrigatório para combinar áudio e vídeo

---

## 📌 Melhorias futuras
- 📊 Barra de progresso de download
- 🎵 Download apenas de áudio (MP3)
- 📂 Escolha de diretório pelo usuário
- 🖥️ Player embutido (WebView2)
- 🔄 Download em lote