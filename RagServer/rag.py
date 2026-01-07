import logging
import os
import faiss
from utils import make_or_open_vec_db, get_base_path
from langchain_community.embeddings import HuggingFaceEmbeddings
from langchain_community.vectorstores import FAISS
from langchain.text_splitter import RecursiveCharacterTextSplitter
from pypdf import PdfReader

logger = logging.getLogger(__name__)

class VectorBase:
    def __init__(self):
        self.path = make_or_open_vec_db()

       
        self.embedding_model_path = self._resolve_embedding_path()
        self.embeddings = HuggingFaceEmbeddings(
            model_name=self.embedding_model_path
        )

        
        self.text_splitter = RecursiveCharacterTextSplitter(
            chunk_size=320,
            chunk_overlap=40
        )

        
        self.index_path = os.path.join(self.path, "faiss_index")
        if os.path.exists(self.index_path):
            logger.info("Loading existing FAISS index...")
            self.vectorstore = FAISS.load_local(
                self.index_path,
                self.embeddings,
                allow_dangerous_deserialization=True
            )
        else:
            logger.info("Creating new FAISS index...")
            self.vectorstore = None 

    def _resolve_embedding_path(self):
        base = os.path.join(get_base_path(), "embedding_model")

        if not os.path.exists(base):
            raise FileNotFoundError(f"Embedding model directory not found: {base}")

        snapshots = os.listdir(base)
        if not snapshots:
            raise FileNotFoundError(f"No model snapshot found inside {base}")

        model_path = os.path.join(base, snapshots[-1])
        logger.info(f"Embedding model loaded: {model_path}")
        return model_path

    def add_document(self, document_name):
        if not os.path.exists(document_name):
            raise FileNotFoundError(f"Document does not exist: {document_name}")

        reader = PdfReader(document_name)
        logger.info(f"Reading {len(reader.pages)} pages from {document_name}")

        all_docs = []

        for idx, page in enumerate(reader.pages, start=1):
            text = page.extract_text() or ""
            if not text.strip():
                continue

            docs = self.text_splitter.create_documents(
                [text.strip()],
                metadatas=[{"source": document_name, "page no": idx}],
            )
            all_docs.extend(docs)

        if self.vectorstore is None:
            self.vectorstore = FAISS.from_documents(all_docs, self.embeddings)
        else:
            self.vectorstore.add_documents(all_docs)

        # Persist FAISS index
        self.vectorstore.save_local(self.index_path)
        logger.info("FAISS index persisted.")

    def query(self, query_text, top_k=3):
        logger.info(f"Query: {query_text}")

        if self.vectorstore is None:
            raise ValueError("FAISS index not initialized. Add a document first.")

        results = self.vectorstore.similarity_search_with_score(
            query_text, k=top_k
        )

        return results

